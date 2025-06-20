using AuthService.Enums;
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
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordHasher<Person> _passwordHasher;
    
    public AccountService(IAuthRepository authRepository, IEncryptionService encryptionService, JwtTokenService jwtTokenService, ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
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
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            var checkNumberPhone = await _authRepository.GetUserByPhoneNumberAsync(phoneNumber);
            if (checkNumberPhone != null)
                return new BaseResponse<string, string> { Message = "Данный номер телефона занят", Successfully = false, Status = 403, Type = ResponseType.PhoneNumberInUse, Errors = "Forbidden", Data = null };
            
            var person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
            if (person == null)
                return new BaseResponse<string, string> { Message = "Данный пользователь не найден", Successfully = false, Status = 404, Type = ResponseType.PersonNotFound, Errors = "Not Found", Data = null };
            
            person.NumberPhone = phoneNumber;
            await _authRepository.SaveChangesAsync();
            
            return new BaseResponse<string, string> { Message = "Номер телефона успешно обновлен", Successfully = true, Status = 200, Type = ResponseType.Ok, Errors = null, Data = "Номер телефона обновлен" };
        }
        return new BaseResponse<string, string> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
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
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            var person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
            if (person == null)
                return new BaseResponse<string, RegistrationCode> { Message = "Данный пользователь не найден", Successfully = false, Status = 404, Type = ResponseType.PersonNotFound, Errors = "Not Found", Data = null };
            
            if (_passwordHasher.VerifyHashedPassword(person, person.PasswordHash, oldPassword) != PasswordVerificationResult.Success)
                return new BaseResponse<string, RegistrationCode> { Message = "Данный пароль не верен", Successfully = false, Status = 403, Type = ResponseType.InvalidPasswordError, Errors = "Forbidden", Data = null };
            
            if (oldPassword == newPassword)
                return new BaseResponse<string, RegistrationCode> { Message = "Данный пароль уже используется", Successfully = false, Status = 403, Type = ResponseType.InvalidPasswordError, Errors = "Forbidden", Data = null };
            
            person.PasswordHash = _passwordHasher.HashPassword(person, newPassword);
            await RevokeAllSessions(person.PersonId);
            await _authRepository.SaveChangesAsync();

            var code = await _authRepository.GenerateCodeAndSaveAsync(person, userIpAddress, _encryptionService.Decrypt(person.TotpCode));
            var codeResult = new RegistrationCode { Code = code, ExpiresAt = DateTime.UtcNow.AddMinutes(AuthRepository.CodeLifetimeMinute) };
            var result = new BaseResponse<string, RegistrationCode> { Message = "Остался всего один шаг", Successfully = true, Status = 200, Type = ResponseType.Ok, Data = codeResult, Errors = null };
            return result;
        }
        return new BaseResponse<string, RegistrationCode> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
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