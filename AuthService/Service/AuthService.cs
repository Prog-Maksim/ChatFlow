using AuthService.Enums;
using AuthService.Extensions;
using AuthService.Models.DB;
using AuthService.Models.Other;
using AuthService.Models.Requests;
using AuthService.Models.Response;
using AuthService.Monitoring;
using AuthService.Repository;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.AspNetCore.Identity;
using UAParser;

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
        
        var user = new Person
        {
            PersonId = Guid.NewGuid().ToString(),
            NumberPhone = registrationUser.Login,
            PasswordVersion = 1,
            RegistrationIp = _encryptionService.Encrypt(userIpAddress),
            AccountState = AccountState.Registration
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, registrationUser.Password);

        await _authRepository.AddUserAsync(user);
        await _authRepository.SaveChangesAsync();
            
        var code = await _authRepository.GenerateCodeAndSaveAsync(user.PersonId, user, userIpAddress);

        var codeResult = new RegistrationCode { Code = code, ExpiresAt = DateTime.UtcNow.AddMinutes(AuthRepository.CodeLifetimeMinute) };
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
            
            
            var code = await _authRepository.GenerateCodeAndSaveAsync(person.PersonId, person, userIpAddress, _encryptionService.Decrypt(person.TotpCode));
            
            var codeResult = new RegistrationCode { Code = code, ExpiresAt = DateTime.UtcNow.AddMinutes(AuthRepository.CodeLifetimeMinute) };
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
    /// <param name="userIpAddress">IP адрес пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, Token2Fa>> AddGoogleAuthenticatorAsync(string code, string userIpAddress)
    {
        if (await _authRepository.CheckCodeAsync(code))
        {
            var data = await _authRepository.GetTotpDataByCodeAsync(code);

            if (data == null || data.IpAdress != userIpAddress)
                return new BaseResponse<string, Token2Fa>
                {
                    Message = "Отказано", Successfully = false, Status = 403, Type = ResponseType.AccessDenied,
                    Errors = "Forbidden", Data = null
                };
            
            if (data.TotpCode != null && !data.IsUpdate)
                return new BaseResponse<string, Token2Fa> 
                { 
                    Message = "Код уже был создан", Successfully = false, Status = 403, Type = ResponseType.CodeIsCreated, 
                    Errors = "Forbidden", Data = null 
                };
            
            var key = GoogleAuthenticatorService.GenerateKey();
            await _authRepository.UpdateTotpDataByCodeAsync(code, key);
            
            var codeResult = new Token2Fa { Token = key };
            var result = new BaseResponse<string, Token2Fa>
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
        return new BaseResponse<string, Token2Fa> { Message = "Данный код не найден", Successfully = false, Status = 404, Type = ResponseType.CodeNotFount, Errors = "Not Found", Data = null};
    }

    /// <summary>
    /// Проверяет код и выдает токены
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <param name="key">Код из Google Authenticator</param>
    /// <param name="userIpAddress">IP адрес пользователя</param>
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

            if (await _authRepository.GetNumberSessionsAsync(data.PersonId) >= 5)
                return new BaseResponse<string, AuthTokens> { Message = "Достигнуто максимальное кол-во устройств", Successfully = false, Status = 403, Type = ResponseType.DeviceLimitReached, Errors = "Forbidden", Data = null};
            
            if (data.IpAdress != userIpAddress)
                return new BaseResponse<string, AuthTokens>
                {
                    Message = "Отказано", Successfully = false, Status = 403, Type = ResponseType.AccessDenied,
                    Errors = "Forbidden", Data = null
                };

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

                var tokensResult = new AuthTokens
                {
                    AccessToken = tokens.AccessToken, 
                    RefreshToken = tokens.RefreshToken, 
                    AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute),
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(JwtTokenService.RefreshTokenLifetimeDay)
                };
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
            var session = await _authRepository.GetSessionByIdAsync(person.PersonId, dataToken.SessionId);

            if (session == null || session.IsRevoked)
                return new BaseResponse<string, AuthTokens>
                {
                    Message = "Отказано",
                    Successfully = false,
                    Status = 403,
                    Type = ResponseType.AccessDenied,
                    Errors = "Forbidden",
                    Data = null
                };
            
            session.LastUsedAt = DateTime.UtcNow;
            await _authRepository.SaveChangesAsync();
            
            var tokens = _jwtTokenService.CreateJwtToken(person.PersonId, person.PasswordVersion, session.SessionId, session.Id, refreshToken);
            
            var tokensResult = new AuthTokens 
            { 
                AccessToken = tokens.AccessToken, 
                RefreshToken = tokens.RefreshToken, 
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(JwtTokenService.RefreshTokenLifetimeDay)
            };
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

    public async Task<BaseResponse<string, List<string>>> RevokeSession(string accessToken, string? sessionId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);

        if (await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            var sessions = await _authRepository.GetSessionsAsync(dataToken.PersonId);

            if (sessions == null || sessions.Count == 0)
                return new BaseResponse<string, List<string>>
                {
                    Message = "Активные сессии не найдены",
                    Successfully = false,
                    Status = 404,
                    Type = ResponseType.SessionNotFound,
                    Errors = "Not Found",
                    Data = null
                };
            
            if (sessionId != null)
            {
                var session = sessions.FirstOrDefault(s => s.SessionId == sessionId && s.IsRevoked == false);
                if (session == null)
                    return new BaseResponse<string, List<string>>
                    {
                        Message = "Данная сессия не найдена",
                        Successfully = false,
                        Status = 404,
                        Type = ResponseType.SessionNotFound,
                        Errors = "Not Found",
                        Data = null
                    };
                    
                session.IsRevoked = true;
                await _authRepository.AddSessionToBanAsync(sessionId);
                await _authRepository.SaveChangesAsync();

                return new BaseResponse<string, List<string>>
                {
                    Message = "Сессия успешно отозвана",
                    Successfully = true,
                    Status = 200,
                    Type = ResponseType.Ok,
                    Errors = null,
                    Data = new List<string> { sessionId }
                };
            }
            else
            {
                var sessionsRevoke = sessions.Where(s => s.PersonId == dataToken.PersonId && s.Id != dataToken.Id).ToList();
                
                if (sessionsRevoke.Count == 0)
                    return new BaseResponse<string, List<string>>
                    {
                        Message = "Активные сессии не найдены",
                        Successfully = false,
                        Status = 404,
                        Type = ResponseType.SessionNotFound,
                        Errors = "Not Found",
                        Data = null
                    };

                foreach (var session in sessionsRevoke)
                {
                    await _authRepository.AddSessionToBanAsync(session.SessionId);
                    session.IsRevoked = true;
                }
                
                await _authRepository.SaveChangesAsync();
                
                return new BaseResponse<string, List<string>>
                {
                    Message = "Сессии успешно отозваны",
                    Successfully = true,
                    Status = 200,
                    Type = ResponseType.Ok,
                    Errors = null,
                    Data = sessionsRevoke.Select(s => s.SessionId).ToList()
                };
            }
        }
        return new BaseResponse<string, List<string>> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
    }

    /// <summary>
    /// Выдает все активные сессии
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task<BaseResponse<string, List<DataSession>>> GetSessions(string accessToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);

        if (await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            var sessions = await _authRepository.GetSessionsAsync(dataToken.PersonId);
            
            if (sessions == null || sessions.Count == 0)
                return new BaseResponse<string, List<DataSession>>
                {
                    Message = "Сессии не найдены",
                    Successfully = false,
                    Status = 404,
                    Type = ResponseType.SessionNotFound,
                    Errors = "Not Found",
                    Data = null
                };
            
            List<DataSession> result = new List<DataSession>();
            
            foreach (var session in sessions)
            {
                var address = await DeterminingIpAddress.GetPositionUser(session.IpAddress);

                DataSession data = new DataSession
                {
                    IpAddress = session.IpAddress,
                    City = address.City,
                    Country = address.Country,
                    Device = session.Device,
                    Os = session.Os,
                    Browser = session.Browser,
                    CreateAt = session.CreatedAt,
                    LastUsedAt = session.LastUsedAt,
                    SessionId = session.SessionId,
                    IsYou = dataToken.Id == session.Id
                };
                result.Add(data);
            }

            return new BaseResponse<string, List<DataSession>>
            {
                Message = "Ваши активные сессии",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = result
            };
        }
        return new BaseResponse<string, List<DataSession>> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
    }
}