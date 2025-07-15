using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using Microsoft.AspNetCore.Identity;
using Sessions = ChatFlow.Models.DB.Sessions;

namespace ChatFlow.Service;

public class AccountService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly TokenValidator _tokenValidator;
    private readonly PasswordHasher<Persons> _passwordHasher;
    private readonly SessionService _sessionService;
    private readonly ILogger<AccountService> _logger;
    
    public AccountService(IAuthRepository authRepository, IEncryptionService encryptionService, TokenValidator tokenValidator, SessionService sessionService, ILogger<AccountService> logger)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _tokenValidator = tokenValidator;
        _sessionService = sessionService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<Persons>();
    }
    
    /// <summary>
    /// Позволяет обновить номер телефона
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="phoneNumber">Номер телефона</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, string>> UpdateNumberPhone(string accessToken, string phoneNumber)
    {
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(accessToken);
        
        if (!isValid)
            return ResponseFactory.JwtTokenInvalid<string>();
        
        var checkNumberPhone = await _authRepository.GetUserByPhoneNumberAsync(phoneNumber);
        if (checkNumberPhone != null)
            return ResponseFactory.PhoneNumberInUse<string>();
            
        var person = await _authRepository.GetUserByIdAsync(dataToken!.PersonId);
        if (person == null)
            return ResponseFactory.PersonNotFound<string>();
            
        person.NumberPhone = phoneNumber;
        await _authRepository.SaveChangesAsync();
            
        return ResponseFactory.Success("Номер телефона успешно обновлен", "Номер телефона обнолвен");
    }
    
    /// <summary>
    /// Позволяет обновить пароль
    /// </summary>
    /// <param name="accessToken">Access токена</param>
    /// <param name="oldPassword">Старый пароль</param>
    /// <param name="newPassword">Новый пароль</param>
    /// <param name="userIpAddress">Ip адрес пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, RegistrationCode>> UpdatePassword(string accessToken, string oldPassword, string newPassword, string userIpAddress)
    {
        if (await _authRepository.IsBlockedAsync(userIpAddress))
            return ResponseFactory.TooManyRequests<RegistrationCode>();
        
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(accessToken);
        if (!isValid)
            return ResponseFactory.JwtTokenInvalid<RegistrationCode>();
            
        var person = await _authRepository.GetUserByIdAsync(dataToken!.PersonId);
        if (person is null)
            return ResponseFactory.PersonNotFound<RegistrationCode>();

        if (person.PasswordHash is null)
        {
            _logger.LogWarning("У пользователя ({personId}) отсутствует пароль", person.PersonId);
            return ResponseFactory.InvalidPassword<RegistrationCode>("Пароль не найден");
        }
        
        if (_passwordHasher.VerifyHashedPassword(person, person.PasswordHash, oldPassword) != PasswordVerificationResult.Success)
        {
            await _authRepository.IncrementLoginAttemptsAsync(userIpAddress);
            Metrics.TrackFailedLogin(userIpAddress);
            return ResponseFactory.InvalidPassword<RegistrationCode>("Данный пароль не верен");
        }
            
        if (oldPassword == newPassword)
            return ResponseFactory.InvalidPassword<RegistrationCode>("Данный пароль уже используется");
            
        await UpdateUserPasswordAsync(person, newPassword);

        RegistrationCode? codeResult;
        try
        {
            codeResult = await GenerateRegistrationCode(person, userIpAddress);
        }
        catch (NullReferenceException)
        {
            return ResponseFactory.AccountNotActivated<RegistrationCode>();
        }
        
        await _sessionService.RevokeSession(accessToken);
        
        return ResponseFactory.Success("Остался всего один шаг", codeResult);
    }

    /// <summary>
    /// Позволяет выйти пользователю из аккаунта
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, RevokeSession>> ExitTheSession(string accessToken)
    {
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(accessToken);
        if (!isValid)
            return ResponseFactory.JwtTokenInvalid<RevokeSession>();
        
        await _sessionService.RevokeSession(accessToken, dataToken!.SessionId);
        return ResponseFactory.Success("Пользователь успешно вышел из аккаунта", new RevokeSession { SessionId = dataToken.SessionId });
    }
    
    /// <summary>
    /// Обновляет пароль пользователя
    /// </summary>
    /// <param name="person">Объект пользователя</param>
    /// <param name="newPassword">Новый пароль</param>
    private async Task UpdateUserPasswordAsync(Persons person, string newPassword)
    {
        person.PasswordVersion++;
        person.PasswordHash = _passwordHasher.HashPassword(person, newPassword);
        await RevokeAllSessions(person.PersonId);
        await _authRepository.SaveChangesAsync();
    }
    
    /// <summary>
    /// Генерирует регистрационный код
    /// </summary>
    /// <param name="person">Объект пользователя</param>
    /// <param name="ip">IP адрес пользователя</param>
    /// <returns></returns>
    private async Task<RegistrationCode> GenerateRegistrationCode(Persons person, string ip)
    {
        if (person.TotpCode is null)
            throw new NullReferenceException("Отсутствует привязка к сервису двухфакторной аутентификации");
        
        var decryptedTotp = _encryptionService.Decrypt(person.TotpCode);
        var code = await _authRepository.GenerateCodeAndSaveAsync(person, ip, decryptedTotp);
        return new RegistrationCode
        {
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AuthRepository.CodeLifetimeMinute)
        };
    }
        
    /// <summary>
    /// Удаляет все сессии пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    private async Task RevokeAllSessions(string personId)
    {
        IQueryable<Sessions> sessions = _authRepository.GetSessionsAsync(personId);
        if (!sessions.Any()) return;
        
        await _authRepository.RevokeAllSessionsAsync(personId);
        await _authRepository.AddSessionsToBanAsync(sessions.Select(s => s.SessionId), personId);
    }
}