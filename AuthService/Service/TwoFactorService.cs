using AuthService.Enums;
using AuthService.Models.DB;
using AuthService.Models.Response;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using UAParser;

namespace AuthService.Service;

public class TwoFactorService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public TwoFactorService(IAuthRepository authRepository, IEncryptionService encryptionService, JwtTokenService jwtTokenService, ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }
    
    /// <summary>
    /// Создает Secret для добавления в GoogleAuthenticator
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <param name="userIpAddress">IP адрес пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, Token2Fa>> AddGoogleAuthenticatorAsync(string code, string userIpAddress)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (data == null || data.IpAdress != userIpAddress)
                return new BaseResponse<string, Token2Fa> { Message = "Отказано", Successfully = false, Status = 403, Type = ResponseType.AccessDenied, Errors = "Forbidden", Data = null };
            
            if (data.TotpCode != null && !data.IsUpdate)
                return new BaseResponse<string, Token2Fa> { Message = "Код уже был создан", Successfully = false, Status = 403, Type = ResponseType.CodeIsCreated, Errors = "Forbidden", Data = null };
            
            var key = GoogleAuthenticatorService.GenerateKey();
            await _authRepository.UpdateTotpDataByCodeAsync(code, key);
            
            var codeResult = new Token2Fa { Token = key };
            var result = new BaseResponse<string, Token2Fa> { Message = "Ваш код аутентификации", Successfully = true, Status = 200, Type = ResponseType.Ok, Data = codeResult, Errors = null };
        
            return result;
        }
        _logger.LogWarning("Код аутентификации пользователя не найден");
        return new BaseResponse<string, Token2Fa> { Message = "Данный код не найден", Successfully = false, Status = 404, Type = ResponseType.CodeNotFount, Errors = "Not Found", Data = null};
    }

    /// <summary>
    /// Проверяет код и выдает токены
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <param name="key">Код из Google Authenticator</param>
    /// <param name="userIpAddress">IP адрес пользователя</param>
    /// <param name="userAgent">user agent пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, AuthTokens>> CheckGoogleAuthenticatorAsync(string code, string key, string userIpAddress, string userAgent)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (data == null)
            {
                _logger.LogInformation("Пользователь не подключил сервис");
                return new BaseResponse<string, AuthTokens> { Message = "Вы не подключили сервис", Successfully = false, Status = 404, Type = ResponseType.ServiceNotConnected, Errors = "Not Found", Data = null};
            }

            if (await _authRepository.GetNumberSessionsAsync(data.PersonId) >= 10)
                return new BaseResponse<string, AuthTokens> { Message = "Достигнуто максимальное кол-во устройств", Successfully = false, Status = 403, Type = ResponseType.DeviceLimitReached, Errors = "Forbidden", Data = null};
            
            if (data.IpAdress != userIpAddress)
                return new BaseResponse<string, AuthTokens> { Message = "Отказано", Successfully = false, Status = 403, Type = ResponseType.AccessDenied, Errors = "Forbidden", Data = null };

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
                    _logger.LogInformation("Пользователь уже зарегистрирован");

                string sessionId = Guid.NewGuid().ToString();
                
                var parser = Parser.GetDefault();
                ClientInfo clientInfo = parser.Parse(userAgent);

                Session session = new Session
                {
                    PersonId = data.PersonId,
                    SessionId = sessionId,
                    IpAddress = _encryptionService.Encrypt(userIpAddress),
                    Device = clientInfo.Device.ToString(),
                    Os = clientInfo.OS.ToString(),
                    Browser = clientInfo.UA.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsRevoked = false
                };
                await _authRepository.AddSessionAsync(session);
                await _authRepository.SaveChangesAsync();
                
                var tokens = _jwtTokenService.CreateJwtToken(data.PersonId, data.PersonData.PasswordVersion, sessionId, session.Id);
                await _authRepository.DeleteTotpDataByCodeAsync(code);

                var tokensResult = new AuthTokens { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken, AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute), RefreshTokenExpiration = DateTime.UtcNow.AddDays(JwtTokenService.RefreshTokenLifetimeDay) };
                return new BaseResponse<string, AuthTokens> { Message = "Вы успешно авторизовались", Successfully = true, Status = 200, Type = ResponseType.Ok, Errors = null, Data = tokensResult, };
            }
            return new BaseResponse<string, AuthTokens> { Message = "Код не верен", Successfully = false, Status = 403, Type = ResponseType.CodeIsNotValid, Errors = "Forbidden", Data = null};
        }
        _logger.LogWarning("Код аутентификации пользователя не найден");
        return new BaseResponse<string, AuthTokens> { Message = "Данный код не найден", Successfully = false, Status = 404, Type = ResponseType.CodeNotFount, Errors = "Not Found", Data = null};
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
    /// Создает Qr-code для добавления в Google Authenticator
    /// </summary>
    /// <param name="code"></param>
    /// <param name="userIpAddress">IP адрес пользователя</param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task<byte[]> GetQrCodeGoogleAuthenticatorAsync(string code, string userIpAddress)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);
            
            if (data == null)
                throw new NullReferenceException("Вы не создали подключение");
            
            if (data.IpAdress != userIpAddress)
                throw new UnauthorizedAccessException("Qr-code не может быть создан!");
            
            if (!data.IsRead)
                throw new UnauthorizedAccessException("Qr-code не может быть создан!");

            if (data.TotpCode == null)
                throw new NullReferenceException("Подключаемый сервис не найден");


            var userData = data.PersonData.Email ?? data.PersonData.NumberPhone;
            string url = GoogleAuthenticatorService.GenerateUrl(data.TotpCode, userData);
            return GoogleAuthenticatorService.GenerateQrCode(url);
        }
        throw new NullReferenceException("Данный код не найден");
    }
}