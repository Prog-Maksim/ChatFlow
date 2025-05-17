using AuthService.Enums;
using AuthService.Extensions;
using AuthService.Models.DB;
using AuthService.Models.Other;
using AuthService.Models.Requests;
using AuthService.Models.Response;
using AuthService.Monitoring;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.AspNetCore.Identity;

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
    public async Task<BaseResponse<string, RegistrationCode>> RegistrationUserAsync(RegistrationUser registrationUser, string userIpAddress)
    {
        _logger.LogInformation("Начало регистрации нового пользователя");
        
        var person = await _authRepository.GetUserByPhoneNumberAsync(registrationUser.Login);
        
        if (person != null)
            return new BaseResponse<string, RegistrationCode> { Message = "Данный номер телефона занят", Successfully = false, Type = ResponseType.PhoneNumberInUse, Status = 403, Errors = "Forbidden", Data = null};

        PersonRegion region = await DeterminingIpAddress.GetPositionUser(userIpAddress);
        
        var user = new Person
        {
            PersonId = Guid.NewGuid().ToString(),
            NumberPhone = registrationUser.Login,
            PasswordVersion = 1,
            RegistrationIp = _encryptionService.Encrypt(userIpAddress),
            RegistrationCity = _encryptionService.Encrypt(region.City),
            RegistrationCountry = _encryptionService.Encrypt(region.Country),
            AccountState = AccountState.Registration
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, registrationUser.Password);

        await _authRepository.AddUserAsync(user);
        await _authRepository.SaveChangesAsync();
            
        var code = await _authRepository.GenerateCodeAndSaveAsync(user.PersonId, user);

        var codeResult = new RegistrationCode { Code = code };
        var result = new BaseResponse<string, RegistrationCode>
        {
            Message = "Пользователь успешно создан",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Data = codeResult,
            Errors = null
        };
        
        return result;
    }

    /// <summary>
    ///  Авторизирует пользователя
    /// </summary>
    /// <param name="login">Номер телефона или почта</param>
    /// <param name="password">Пароль</param>
    /// <param name="userIpAddress">Ip адрес пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, RegistrationCode>> AuthorizationUserAsync(string login, string password, string userIpAddress)
    {
        if (await _authRepository.IsBlockedAsync(userIpAddress))
            return new BaseResponse<string, RegistrationCode>
            {
                Status = 429, 
                Message = "Слишком много попыток входа. Попробуйте еще раз позже.", 
                Type = ResponseType.TooManyRequests, 
                Errors = "Too Many Requests", 
                Successfully = false, 
                Data = null
            };
        
        _logger.LogInformation("Начало авторизации пользователя");

        if (login.IsNumberPhone())
        {
            var person = await _authRepository.GetUserByPhoneNumberAsync(login);
            
            if (person == null)
                return new BaseResponse<string, RegistrationCode>{Status = 404, Message = "Пользователь не найден", Errors = "Not Found", Type = ResponseType.PersonNotFound, Successfully = false, Data = null };
            
            if (person.AccountState == AccountState.Registration)
                return new BaseResponse<string, RegistrationCode>{Status = 403, Message = "Требуется сначало завершить регистрацию аккаунта", Type = ResponseType.AccountRegistrationIncomplete, Errors = "Forbidden", Successfully = false, Data = null };
            
            if (person.AccountState == AccountState.Blocked)
                return new BaseResponse<string, RegistrationCode>{Status = 403, Message = "Данный аккаунт заблокирован", Errors = "Forbidden", Type = ResponseType.AccountIsBlocked, Successfully = false, Data = null };

            if (_passwordHasher.VerifyHashedPassword(person, person.PasswordHash, password) != PasswordVerificationResult.Success)
            {
                await _authRepository.IncrementLoginAttemptsAsync(userIpAddress);
                TrackFailedLogin(userIpAddress);
                return new BaseResponse<string, RegistrationCode>{ Status = 403, Message = "Пароль не верен", Type = ResponseType.InvalidPasswordError, Errors = "Forbidden", Successfully = false, Data = null };
            }
            
            
            var code = await _authRepository.GenerateCodeAndSaveAsync(person.PersonId, person, _encryptionService.Decrypt(person.TotpCode));
            
            var codeResult = new RegistrationCode { Code = code };
            var result = new BaseResponse<string, RegistrationCode>
            {
                Message = "Последний шаг, подтвердите личность",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Data = codeResult,
                Errors = null
            };
        
            return result;
        }
        if (login.IsEmail())
            return new BaseResponse<string, RegistrationCode>{ Status = 400, Message = "Вход по электронной почте сейчас не поддерживается", Type = ResponseType.EmailLoginNotSupported, Errors = "Bad Request", Successfully = false, Data = null};
        
        return new BaseResponse<string, RegistrationCode>{ Status = 400, Message = "Некорректная строка", Type = ResponseType.InvalidString, Errors = "Bad Request", Successfully = false, Data = null };
    }
    
    private void TrackFailedLogin(string ip)
    {
        MetricsRegistry.BruteForceDetection
            .WithLabels(ip, "auth", Environment.MachineName)
            .Inc();
    }
    
    /// <summary>
    /// Создает Secret для добавления в GoogleAuthenticator
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, RegistrationCode>> AddGoogleAuthenticatorAsync(string code)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (data.TotpCode != null && !data.IsUpdate)
                return new BaseResponse<string, RegistrationCode> { Message = "Код уже был создан", Successfully = false, Status = 403, Type = ResponseType.CodeIsCreated, Errors = "Forbidden", Data = null };
            
            var key = GoogleAuthenticatorService.GenerateKey();
            await _authRepository.UpdateTotpDataByCodeAsync(code, key);
            
            var codeResult = new RegistrationCode { Code = key };
            var result = new BaseResponse<string, RegistrationCode>
            {
                Message = "Ваш код аутентификации",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Data = codeResult,
                Errors = null
            };
        
            return result;
        }
        _logger.LogWarning("Код аутентификации пользователя не найден");
        return new BaseResponse<string, RegistrationCode> { Message = "Данный код не найден", Successfully = false, Status = 404, Type = ResponseType.CodeNotFount, Errors = "Not Found", Data = null};
    }

    /// <summary>
    /// Проверяет код и выдает токены
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <param name="key">Код из Google Authenticator</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, AuthTokens>> CheckGoogleAuthenticatorAsync(string code, string key)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (data == null)
            {
                _logger.LogInformation("Пользователь не подключил сервис");
                return new BaseResponse<string, AuthTokens> { Message = "Вы не подключили сервис", Successfully = false, Status = 404, Type = ResponseType.ServiceNotConnected, Errors = "Not Found", Data = null};
            }

            if (data.TotpCode == null)
            {
                _logger.LogWarning("Секрет TOTP не найден");
                return new BaseResponse<string, AuthTokens> { Message = "Подключаемый сервис не найден", Successfully = false, Status = 404, Type = ResponseType.ServiceConnectedNotFound, Errors = "Not Found", Data = null};
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

                var tokensResult = new AuthTokens { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken};
                return new BaseResponse<string, AuthTokens>
                {
                    Message = "Вы успешно авторизовались",
                    Successfully = true,
                    Status = 200,
                    Type = ResponseType.Ok,
                    Errors = null,
                    Data = tokensResult,
                };
            }
            return new BaseResponse<string, AuthTokens> { Message = "Код не верен", Successfully = false, Status = 403, Type = ResponseType.CodeIsNotValid, Errors = "Forbidden", Data = null};
        }
        _logger.LogWarning("Код аутентификации пользователя не найден");
        return new BaseResponse<string, AuthTokens> { Message = "Данный код не найден", Successfully = false, Status = 404, Type = ResponseType.CodeNotFount, Errors = "Not Found", Data = null};
    }

    /// <summary>
    /// Создает Qr-code для добавления в Google Authenticator
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task<byte[]> GetQrCodeGoogleAuthenticatorAsync(string code)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (!data.IsRead)
                throw new UnauthorizedAccessException("Qr-code не может быть создан!");
            
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

    /// <summary>
    /// Обновляет Refesh токен
    /// </summary>
    /// <param name="refreshToken">refresh токен</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, AuthTokens>> RefreshAccessToken(string refreshToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(refreshToken);
        var person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
        
        if (person == null || person.AccountState == AccountState.Blocked)
            return new BaseResponse<string, AuthTokens> { Message = "Пользователь не найден или был заблокирован!", Type = ResponseType.PersonNotFoundOrBlocked, Errors = "Forbidden", Status = 423, Successfully = false, Data = null};

        if (await _jwtTokenService.ValidateJwtRefreshToken(dataToken, person))
        {
            var tokens = _jwtTokenService.CreateJwtToken(person.PersonId, person.PasswordVersion, refreshToken);
            
            var tokensResult = new AuthTokens { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken};
            return new BaseResponse<string, AuthTokens>
            {
                Message = "Токены успешно обновлены",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = tokensResult,
            };
        }
        return new BaseResponse<string, AuthTokens> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
    }
}