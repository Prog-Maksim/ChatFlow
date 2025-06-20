using AuthService.Models.DB;
using AuthService.Models.Response;
using AuthService.Repository;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Service;

public class AccountService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly TokenValidator _tokenValidator;
    private readonly PasswordHasher<Person> _passwordHasher;
    
    public AccountService(IAuthRepository authRepository, IEncryptionService encryptionService, TokenValidator tokenValidator)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _tokenValidator = tokenValidator;
        _passwordHasher = new PasswordHasher<Person>();
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
            
        var person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
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
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(accessToken);
        if (!isValid)
            return ResponseFactory.JwtTokenInvalid<RegistrationCode>();
            
        var person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
        if (person == null)
            return ResponseFactory.PersonNotFound<RegistrationCode>();
            
        if (_passwordHasher.VerifyHashedPassword(person, person.PasswordHash, oldPassword) != PasswordVerificationResult.Success)
            return ResponseFactory.InvalidPassword<RegistrationCode>("Данный пароль не верен");
            
        if (oldPassword == newPassword)
            return ResponseFactory.InvalidPassword<RegistrationCode>("Данный пароль уже используется");
            
        await UpdateUserPasswordAsync(person, newPassword);
        var codeResult = await GenerateRegistrationCode(person, userIpAddress);
        
        return ResponseFactory.Success("Остался всего один шаг", codeResult);
    }

    /// <summary>
    /// Обновляет пароль пользователя
    /// </summary>
    /// <param name="person">Объект пользователя</param>
    /// <param name="newPassword">Новый пароль</param>
    private async Task UpdateUserPasswordAsync(Person person, string newPassword)
    {
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
    private async Task<RegistrationCode> GenerateRegistrationCode(Person person, string ip)
    {
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
        IQueryable<Session> sessions = _authRepository.GetSessionsAsync(personId);
        if (!sessions.Any()) return;
        
        await _authRepository.RevokeAllSessionsAsync(personId);
        await _authRepository.AddSessionsToBanAsync(sessions.Select(s => s.SessionId), personId);
    }
}