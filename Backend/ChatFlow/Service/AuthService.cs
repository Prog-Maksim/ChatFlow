using ChatFlow.Enums;
using ChatFlow.Extensions;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Repository;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using Microsoft.AspNetCore.Identity;

namespace ChatFlow.Service;

public class AuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly PasswordHasher<Persons> _passwordHasher;
    private readonly ISearchRepository _searchRepository;
    
    public AuthService(IAuthRepository authRepository, IEncryptionService encryptionService, ISearchRepository searchRepository)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _passwordHasher = new PasswordHasher<Persons>();
        _searchRepository = searchRepository;
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
        
        var person = await _authRepository.GetUserByPhoneNumberAsync(registrationUser.Login);

        if (person != null)
            return ResponseFactory.PhoneNumberInUse<RegistrationCode>();
        
        var user = CreateUserEntity(registrationUser, userIpAddress);
        await _authRepository.AddUserAsync(user);
        var userData = AddUserDataEntity(registrationUser, user.PersonId);
        await _authRepository.AddUserDataAsync(userData);
        await _authRepository.SaveChangesAsync();
        _ = AddUserToSearch(userData);
            
        var code = await _authRepository.GenerateCodeAndSaveAsync(user, userIpAddress);
        var codeResult = new RegistrationCode { Code = code, ExpiresAt = DateTime.UtcNow.AddMinutes(AuthRepository.CodeLifetimeMinute) };

        return ResponseFactory.Success("Пользователь успешно создан", codeResult);
    }
    
    /// <summary>
    /// Создает объект пользователя
    /// </summary>
    /// <param name="registrationUser">Данные пользователя</param>
    /// <param name="userIpAddress">IP адрес</param>
    /// <returns></returns>
    private Persons CreateUserEntity(RegistrationUser registrationUser, string userIpAddress)
    {
        var user = new Persons
        {
            PersonId = Guid.NewGuid().ToString(),
            NumberPhone = registrationUser.Login,
            PasswordVersion = 1,
            RegistrationIp = _encryptionService.Encrypt(userIpAddress),
            AccountState = AccountState.Registration,
            RegistrationTime = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, registrationUser.Password);
        return user;
    }

    /// <summary>
    /// Создает объект пользователя
    /// </summary>
    /// <param name="registrationUser">Данные пользователя</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <returns></returns>
    private DataPersons AddUserDataEntity(RegistrationUser registrationUser, string userId)
    {
        Random rnd = new Random();
        
        var user = new DataPersons
        {
            PersonId = userId,
            Name = registrationUser.Name,
            Surname = registrationUser.Surname,
            Tag = $"@{rnd.Next(1111, 9999)}-{rnd.Next(1111, 9999)}"
        };
        return user;
    }

    private async Task AddUserToSearch(DataPersons data)
    {
        UserCreated user = new UserCreated
        {
            Name = data.Name,
            Surname = data.Surname,
            Tag = data.Tag,
            PersonId = data.PersonId
        };
        await _searchRepository.CreatePersonAsync(user);
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
            return ResponseFactory.TooManyRequests<RegistrationCode>();

        if (login.IsNumberPhone())
            return await AuthorizeByPhoneAsync(login, password, userIpAddress);

        if (login.IsEmail())
            return ResponseFactory.EmailNotSupported<RegistrationCode>();

        return ResponseFactory.BadRequest<RegistrationCode>("Некорректный логин");
    }
    
    /// <summary>
    /// Авторизация по номеру телефона
    /// </summary>
    /// <param name="phone">Номер телефона</param>
    /// <param name="password">Пароль</param>
    /// <param name="userIpAddress">IP адрес</param>
    /// <returns></returns>
    private async Task<BaseResponse<string, RegistrationCode>> AuthorizeByPhoneAsync(string phone, string password, string userIpAddress)
    {
        var person = await _authRepository.GetUserByPhoneNumberAsync(phone);
        if (person == null)
            return ResponseFactory.PersonNotFound<RegistrationCode>();

        if (person.AccountState == AccountState.Registration)
            return ResponseFactory.AccountNotActivated<RegistrationCode>();

        if (person.AccountState == AccountState.Blocked)
            return ResponseFactory.AccountBlocked<RegistrationCode>();

        if (!IsPasswordValid(person, password))
        {
            await _authRepository.IncrementLoginAttemptsAsync(userIpAddress);
            Metrics.TrackFailedLogin(userIpAddress);
            return ResponseFactory.InvalidPassword<RegistrationCode>("Данный пароль не верен");
        }
        
        var code = await _authRepository.GenerateCodeAndSaveAsync(person, userIpAddress, _encryptionService.Decrypt(person.TotpCode!));
        var codeResult = new RegistrationCode
        {
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AuthRepository.CodeLifetimeMinute)
        };

        return ResponseFactory.Success("Последний шаг, подтвердите личность", codeResult);
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