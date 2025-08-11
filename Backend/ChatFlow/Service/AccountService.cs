using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Scripts.Interfaces;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ChatFlow.Service;

public class AccountService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenValidator _tokenValidator;
    private readonly PasswordHasher<Persons> _passwordHasher;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AccountService> _logger;
    
    public AccountService(IAuthRepository authRepository, ITokenValidator tokenValidator, ISessionService sessionService, ILogger<AccountService> logger)
    {
        _authRepository = authRepository;
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
            
        return ResponseFactory.Success("Номер телефона успешно обновлен", "Номер телефона обновлен");
    }
    
    /// <summary>
    /// Позволяет обновить пароль
    /// </summary>
    /// <param name="accessToken">Access токена</param>
    /// <param name="oldPassword">Старый пароль</param>
    /// <param name="newPassword">Новый пароль</param>
    /// <param name="userIpAddress">Ip адрес пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, string>> UpdatePassword(string accessToken, string oldPassword, string newPassword, string userIpAddress)
    {
        if (await _authRepository.IsBlockedAsync(userIpAddress))
            return ResponseFactory.TooManyRequests<string>();
        
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(accessToken);
        if (!isValid)
            return ResponseFactory.JwtTokenInvalid<string>();
            
        var person = await _authRepository.GetUserByIdAsync(dataToken!.PersonId);
        if (person is null)
            return ResponseFactory.PersonNotFound<string>();
        
        if (person.AccountState == AccountState.Blocked)
            return ResponseFactory.AccountBlocked<string>();

        if (person.PasswordHash is null)
        {
            _logger.LogWarning("У пользователя ({personId}) отсутствует пароль", person.PersonId);
            return ResponseFactory.InvalidPassword<string>("Пароль не найден");
        }
        
        if (_passwordHasher.VerifyHashedPassword(person, person.PasswordHash, oldPassword) != PasswordVerificationResult.Success)
        {
            await _authRepository.IncrementLoginAttemptsAsync(userIpAddress);
            Metrics.TrackFailedLogin(userIpAddress);
            return ResponseFactory.InvalidPassword<string>("Данный пароль не верен");
        }
            
        if (oldPassword == newPassword)
            return ResponseFactory.InvalidPassword<string>("Данный пароль уже используется");

        await UpdateUserPasswordAsync(person.PersonId, newPassword);
        await _sessionService.RevokeSession(accessToken);
        
        return ResponseFactory.Success("Вы успешно обновили пароль", "успешно");
    }
    
    /// <summary>
    /// Обновляет пароль пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="newPassword">Новый пароль</param>
    private async Task UpdateUserPasswordAsync(string personId, string newPassword)
    {
        var person = await _authRepository.GetUserByIdAsync(personId);
        
        if (person is null)
            return;
        
        person.PasswordVersion++;
        person.PasswordHash = newPassword;
        await _authRepository.SaveChangesAsync();
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
}