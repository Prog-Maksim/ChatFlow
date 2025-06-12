using AuthService.Enums;
using AuthService.Extensions;
using AuthService.Models.DB;
using AuthService.Models.Events;
using AuthService.Models.Requests;
using AuthService.Models.Response;
using AuthService.Monitoring;
using AuthService.Repository;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Service;

public class AuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordHasher<Person> _passwordHasher;
    private readonly KafkaEventProducer _kafka;

    public AuthService(IAuthRepository authRepository, IEncryptionService encryptionService, KafkaEventProducer kafka, ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _logger = logger;
        _kafka = kafka;
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
        if (!registrationUser.Login.IsNumberPhone())
            return new BaseResponse<string, RegistrationCode> { Message = "Некорректный формат номера телефона", Successfully = false, Type = ResponseType.PhoneNumberNotValid, Status = 400, Errors = "Bad Request", Data = null };
        
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
            AccountState = AccountState.Registration,
            RegistrationTime = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, registrationUser.Password);

        await _authRepository.AddUserAsync(user);
        await _authRepository.SaveChangesAsync();
            
        var code = await _authRepository.GenerateCodeAndSaveAsync(user, userIpAddress);

        var codeResult = new RegistrationCode { Code = code, ExpiresAt = DateTime.UtcNow.AddMinutes(AuthRepository.CodeLifetimeMinute) };
        var result = new BaseResponse<string, RegistrationCode> { Message = "Пользователь успешно создан", Successfully = true, Status = 200, Type = ResponseType.Ok, Data = codeResult, Errors = null };

        var newUser = new UserCreated
        {
            PersonId = user.PersonId,
            Name = registrationUser.Name,
            Surname = registrationUser.Surname
        };
        await _kafka.PublishUserCreatedAsync(newUser);
        
        return result;
    }

    /// <summary>
    ///  Авторизация пользователя
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
            
            var code = await _authRepository.GenerateCodeAndSaveAsync(person, userIpAddress, _encryptionService.Decrypt(person.TotpCode));
            
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
    
    /// <summary>
    /// Мониторинг неправильных попыток входа
    /// </summary>
    /// <param name="ip">Ip адрес пользовтаеля</param>
    private void TrackFailedLogin(string ip)
    {
        MetricsRegistry.BruteForceDetection
            .WithLabels(ip, "auth", Environment.MachineName)
            .Inc();
    }
}