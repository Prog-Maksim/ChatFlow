using System.Net;
using AuthService.Enums;
using AuthService.Models.DB;
using AuthService.Models.Other;
using AuthService.Models.Requests;
using AuthService.Models.Response;
using AuthService.Monitoring;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace AuthService.Service;

public class AuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordHasher<Person> _passwordHasher;

    public AuthService(IAuthRepository authRepository, IEncryptionService encryptionService, JwtTokenService jwtTokenService, ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<Person>();
    }

    /// <summary>
    /// Регистрирует нового пользователя
    /// </summary>
    /// <param name="registrationUser">Данные о пользователе</param>
    /// <param name="userIpAddress">Ip адрес пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse> RegistrationUserAsync(RegistrationUser registrationUser, string userIpAddress)
    {
        _logger.LogInformation("Начало регистрации нового пользователя");

        using (AuthMetrics.RegistrationDurationHistogram.NewTimer())
        {
            var person = await _authRepository.GetUserByPhoneNumberAsync(registrationUser.NumberPhone);
        
            if (person != null)
            {
                AuthMetrics.RegistrationFailureCounter.Inc();
                return new BaseResponse { Message = "Данный номер телефона занят", Success = false, StatusCode = 403, Error = "Forbidden" };
            }

            PersonRegion region = await DeterminingIpAddress.GetPositionUser(userIpAddress);
        
            var user = new Person
            {
                PersonId = Guid.NewGuid().ToString(),
                NumberPhone = registrationUser.NumberPhone,
                PasswordVersion = 1,
                RegistrationIp = _encryptionService.Encrypt(userIpAddress),
                RegistrationCity = _encryptionService.Encrypt(region.City),
                RegistrationCountry = _encryptionService.Encrypt(region.Country),
                AccountState = AccountState.Registration
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, registrationUser.Password);

            await _authRepository.AddUserAsync(user);
            await _authRepository.SaveChangesAsync();
            
            AuthMetrics.RegistrationSuccessCounter.Inc();
            var code = await _authRepository.GenerateCodeAndSaveAsync(user.PersonId, user);
        
            return new RegistrationCode { Message = "Пользователь успешно создан", Success = true, StatusCode = 200, Code = code};
        }
    }

    /// <summary>
    /// Создает Secret для добавления в GoogleAuthenticator
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <returns></returns>
    public async Task<BaseResponse> AddGoogleAuthenticatorAsync(string code)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var key = GoogleAuthenticatorService.GenerateKey();
            await _authRepository.UpdateTotpDataByCodeAsync(code, key);
            return new RegistrationCode { Message = "Ваш код аутентификации", Success = true, StatusCode = 200, Code = key };
        }
        _logger.LogWarning("Код аутентификации пользователя не найден");
        return new BaseResponse { Message = "Данный код не найден", Success = false, StatusCode = 404, Error = "Not Found" };
    }

    /// <summary>
    /// Проверяет код и выдает токены
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <param name="key">Код из Google Authenticator</param>
    /// <returns></returns>
    public async Task<BaseResponse> CheckGoogleAuthenticatorAsync(string code, string key)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (data == null)
            {
                _logger.LogInformation("Пользователь не подключил сервис");
                return new BaseResponse { Message = "Вы не создали подключение", Success = false, StatusCode = 404, Error = "Not Found" };
            }

            if (data.TotpCode == null)
            {
                _logger.LogWarning("Секрет TOTP не найден");
                return new BaseResponse { Message = "Подключаемый сервис не найден", Success = false, StatusCode = 404, Error = "Not Found" };
            }         
            
            var result = GoogleAuthenticatorService.CheckValidKey(key, data.TotpCode);

            if (result)
            {
                if (data.TotpCode != null)
                {
                    Person? person = await _authRepository.GetUserByIdAsync(data.PersonId);
                    if (person != null)
                        await AddTotpCode(person, data.TotpCode);
                }
                else
                {
                    _logger.LogInformation("Пользователь уже зарегистрирован");
                }
                
                var tokens = _jwtTokenService.CreateJwtToken(data.PersonId, data.PersonData.PasswordVersion);
                await _authRepository.DeleteTotpDataByCodeAsync(code);

                return new AuthTokens { Message = "Вы успешно авторизовались", Success = true, StatusCode = 200, AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken};
            }
            
            return new CheckAuthenticationResult { Message = "Код не верен", Success = false, StatusCode = 403, Result = false, Error = "Forbidden" };
        }
        _logger.LogWarning("Код аутентификации пользователя не найден");
        return new BaseResponse { Message = "Данный код не найден", Success = false, StatusCode = 404, Error = "Not Found" };
    }

    /// <summary>
    /// Создает Qr-code для добавления в Google Authenticator
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<byte[]> GetQrCodeGoogleAuthenticatorAsync(string code)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (data == null)
                throw new NullReferenceException("Вы не создали подключение");

            if (data.TotpCode == null)
                throw new NullReferenceException("Подключаемый сервис не найден");


            var userData = data.PersonData.Email ?? data.PersonData.NumberPhone;
            string url = GoogleAuthenticatorService.GenerateUrl(data.TotpCode, userData);
            return GoogleAuthenticatorService.GenerateQrCode(url);
        }
        

        throw new NullReferenceException("Данный код не найден");
    }
    
    private async Task AddTotpCode(Person person, string totpCode)
    {
        try
        {
            person.TotpCode = _encryptionService.Encrypt(totpCode);
            person.AccountState = AccountState.Active;
            await _authRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Произошла ошибка {@ex}", ex);
        }
    }
}