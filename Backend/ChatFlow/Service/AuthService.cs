using ChatFlow.Enums;
using ChatFlow.Extensions;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Monitoring;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Scripts.Interfaces;
using ChatFlow.Service.Interfaces;
using ChatFlow.Service.Other;
using Microsoft.AspNetCore.Identity;
using Sessions = ChatFlow.Models.DB.Sessions;

namespace ChatFlow.Service;

public class AuthService: IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly PasswordHasher<Persons> _passwordHasher;
    private readonly ISearchRepository _searchRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtherPersonDataRepository _otherPersonDataRepository;

    private const int MaxDevice = 3;
    
    public AuthService(IAuthRepository authRepository, IEncryptionService encryptionService, ISearchRepository searchRepository,  IOtherPersonDataRepository otherPersonDataRepository, IJwtTokenService jwtTokenService)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _passwordHasher = new PasswordHasher<Persons>();
        _searchRepository = searchRepository;
        _otherPersonDataRepository = otherPersonDataRepository;
        _jwtTokenService = jwtTokenService;
    }
    
    public async Task<BaseResponse<string, string>> RegistrationUserAsync(RegistrationUser registrationUser, string userIpAddress)
    {
        if (!registrationUser.Login.IsNumberPhone())
            return new BaseResponse<string, string> { Message = "Некорректный формат номера телефона", Successfully = false, Type = ResponseType.PhoneNumberNotValid, Status = 400, Errors = "Bad Request", Data = null };
        
        var person = await _authRepository.GetUserByPhoneNumberAsync(registrationUser.Login);

        if (person != null)
            return ResponseFactory.PhoneNumberInUse<string>();
        
        var encryptedIp = _encryptionService.Encrypt(userIpAddress); 
        var passwordHash = _passwordHasher.HashPassword(null, registrationUser.Password);
        var user = AuthMapper.CreateUserEntity(registrationUser, encryptedIp, passwordHash);
        
        await _authRepository.AddUserAsync(user);
        var userData = AuthMapper.AddUserDataEntity(registrationUser, user.PersonId);
        await _otherPersonDataRepository.InitializePersonData(user.PersonId);
        await _authRepository.AddUserDataAsync(userData);
        await _authRepository.SaveChangesAsync();

        UserCreated userCreated = AuthMapper.AddUserToSearch(userData);
        _ = _searchRepository.CreatePersonAsync(userCreated);
        
        MetricsRegistry.UserCreationCounter.Inc();

        return ResponseFactory.Success("Пользователь успешно создан", "Успешно");
    }
    
    public async Task<BaseResponse<string, AuthTokens>> AuthorizationUserAsync(string login, string password, string userIpAddress, string publicKey, string userAgent, string? refreshToken = null)
    {
        if (await _authRepository.IsBlockedAsync(userIpAddress))
            return ResponseFactory.TooManyRequests<AuthTokens>();

        if (login.IsNumberPhone())
            return await AuthorizeByPhoneAsync(login, password, userIpAddress, publicKey, userAgent, refreshToken);

        if (login.IsEmail())
            return ResponseFactory.EmailNotSupported<AuthTokens>();

        return ResponseFactory.BadRequest<AuthTokens>("Некорректный логин");
    }
    
    /// <summary>
    /// Авторизация по номеру телефона
    /// </summary>
    /// <param name="phone">Номер телефона</param>
    /// <param name="password">Пароль</param>
    /// <param name="userIpAddress">IP адрес</param>
    /// <param name="publicKey"></param>
    /// <param name="userAgent"></param>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    private async Task<BaseResponse<string, AuthTokens>> AuthorizeByPhoneAsync(string phone, string password, string userIpAddress, string publicKey, string userAgent, string? refreshToken = null)
    {
        var person = await _authRepository.GetUserByPhoneNumberAsync(phone);
        if (person == null)
            return ResponseFactory.PersonNotFound<AuthTokens>();

        if (person.AccountState == AccountState.Blocked)
            return ResponseFactory.AccountBlocked<AuthTokens>();
        
        if (await _authRepository.GetNumberSessionsAsync(person.PersonId) >= MaxDevice)
            return ResponseFactory.Forbidden<AuthTokens>("Достигнуто максимальное количество устройств", ResponseType.DeviceLimitReached);

        if (!IsPasswordValid(person, password))
        {
            await _authRepository.IncrementLoginAttemptsAsync(userIpAddress);
            Metrics.TrackFailedLogin(userIpAddress);
            return ResponseFactory.InvalidPassword<AuthTokens>("Данный пароль не верен");
        }

        // Восстановить сессию
        if (refreshToken is not null)
        {
            var dataToken = _jwtTokenService.GetJwtTokenData(refreshToken);
            if (dataToken.TokenType != TokenType.RefreshToken)
                return new BaseResponse<string, AuthTokens> 
                    { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

            Sessions? session = await _authRepository.GetSessionByIdAsync(person.PersonId, dataToken.SessionId);
            
            if (session is null)
                return ResponseFactory.Forbidden<AuthTokens>("Не удалось восстановить сессию", ResponseType.SessionNotFound);
            
            session.IsRevoked = false;
            session.RevokedAt = null;
            
            await _authRepository.SaveChangesAsync();
            await _otherPersonDataRepository.UpdatePublicKeyStatus(person.PersonId, session.DeviceId, true);
            
            var tokenData = _jwtTokenService.CreateJwtToken(person.PersonId, person.PasswordVersion, session.SessionId, dataToken.DeviceId, session.Id);
            AuthTokens tokens = AuthMapper.GenerateToken(tokenData, person.PersonId, session);
            
            return ResponseFactory.Success("Вы успешно авторизовались", tokens);
        }
        // Создать новую сессию, новый идентификатор устройства
        else
        {
            if (!AuthValidator.IsValidRsaPublicKey(publicKey))
                return ResponseFactory.Forbidden<AuthTokens>("Публичный ключ не является ключем RSA", ResponseType.KeyIsNotRSA);
            
            if(AuthValidator.IsPrivateKey(publicKey))
                return ResponseFactory.Forbidden<AuthTokens>("Данный ключ является приватным", ResponseType.KeyIsNotPublic);
            
            string deviceId = Guid.NewGuid().ToString();
            await _otherPersonDataRepository.AddPublicKey(person.PersonId, publicKey, deviceId);

            var encryptedIp = _encryptionService.Encrypt(userIpAddress); 
            Sessions session = AuthMapper.CreateSession(person.PersonId, encryptedIp, deviceId, userAgent);
            
            await _authRepository.AddSessionAsync(session);
            await _authRepository.SaveChangesAsync();
            
            var tokenData = _jwtTokenService.CreateJwtToken(person.PersonId, person.PasswordVersion, session.SessionId, deviceId, session.Id);
            AuthTokens tokens = AuthMapper.GenerateToken(tokenData, person.PersonId, session);
            
            return ResponseFactory.Success("Вы успешно авторизовались", tokens);
        }
    }

    /// <summary>
    /// Проверка пароля
    /// </summary>
    /// <param name="person">Объект пользователя</param>
    /// <param name="password">Пароль</param>
    /// <returns>True - пароль валиден</returns>
    private bool IsPasswordValid(Persons person, string password)
    {
        return _passwordHasher.VerifyHashedPassword(person, person.PasswordHash!, password) == PasswordVerificationResult.Success;
    }
}