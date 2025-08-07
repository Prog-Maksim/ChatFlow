using ChatFlow.Enums;
using ChatFlow.Extensions;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Repository;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using UAParser;
using Sessions = ChatFlow.Models.DB.Sessions;

namespace ChatFlow.Service;

public class AuthService: IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly PasswordHasher<Persons> _passwordHasher;
    private readonly ISearchRepository _searchRepository;

    private const int MaxDevice = 3;
    
    public AuthService(IAuthRepository authRepository, IEncryptionService encryptionService, ISearchRepository searchRepository)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _passwordHasher = new PasswordHasher<Persons>();
        _searchRepository = searchRepository;
    }
    
    public async Task<BaseResponse<string, string>> RegistrationUserAsync(RegistrationUser registrationUser, string userIpAddress)
    {
        if (!registrationUser.Login.IsNumberPhone())
            return new BaseResponse<string, string> { Message = "Некорректный формат номера телефона", Successfully = false, Type = ResponseType.PhoneNumberNotValid, Status = 400, Errors = "Bad Request", Data = null };
        
        var person = await _authRepository.GetUserByPhoneNumberAsync(registrationUser.Login);

        if (person != null)
            return ResponseFactory.PhoneNumberInUse<string>();
        
        var user = CreateUserEntity(registrationUser, userIpAddress);
        await _authRepository.AddUserAsync(user);
        var userData = AddUserDataEntity(registrationUser, user.PersonId);
        await _authRepository.AddUserDataAsync(userData);
        await _authRepository.SaveChangesAsync();
        _ = AddUserToSearch(userData);

        return ResponseFactory.Success("Пользователь успешно создан", "Успешно");
    }
    
    public async Task<BaseResponse<string, string>> AuthorizationUserAsync(string login, string password, string userIpAddress, string publicKey, string? refreshToken = null)
    {
        if (await _authRepository.IsBlockedAsync(userIpAddress))
            return ResponseFactory.TooManyRequests<string>();

        if (login.IsNumberPhone())
            return await AuthorizeByPhoneAsync(login, password, userIpAddress, publicKey, refreshToken);

        if (login.IsEmail())
            return ResponseFactory.EmailNotSupported<string>();

        return ResponseFactory.BadRequest<string>("Некорректный логин");
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
            AccountState = AccountState.Active,
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
    /// Авторизация по номеру телефона
    /// </summary>
    /// <param name="phone">Номер телефона</param>
    /// <param name="password">Пароль</param>
    /// <param name="userIpAddress">IP адрес</param>
    /// <param name="publicKey"></param>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    private async Task<BaseResponse<string, string>> AuthorizeByPhoneAsync(string phone, string password, string userIpAddress, string publicKey, string? refreshToken = null)
    {
        var person = await _authRepository.GetUserByPhoneNumberAsync(phone);
        if (person == null)
            return ResponseFactory.PersonNotFound<string>();

        if (person.AccountState == AccountState.Blocked)
            return ResponseFactory.AccountBlocked<string>();
        
        if (await _authRepository.GetNumberSessionsAsync(person.PersonId) >= MaxDevice)
            return ResponseFactory.Forbidden<string>("Достигнуто максимальное количество устройств", ResponseType.DeviceLimitReached);

        if (!IsPasswordValid(person, password))
        {
            await _authRepository.IncrementLoginAttemptsAsync(userIpAddress);
            Metrics.TrackFailedLogin(userIpAddress);
            return ResponseFactory.InvalidPassword<string>("Данный пароль не верен");
        }
        

        return ResponseFactory.Success("Последний шаг, подтвердите личность","");
    }

    /// <summary>
    /// Создает объект сессии
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="ipAddress">IP адрес</param>
    /// <param name="deviceId">Идентификатор устройства</param>
    /// <param name="userAgent">User агенты пользователя</param>
    /// <returns></returns>
    private Sessions CreateSession(string personId, string ipAddress, string deviceId, string userAgent)
    {
        var parser = Parser.GetDefault();
        var clientInfo = parser.Parse(userAgent);
    
        return new Sessions
        {
            PersonId = personId,
            SessionId = Guid.NewGuid().ToString(),
            IpAddress = _encryptionService.Encrypt(ipAddress),
            Device = clientInfo.Device.ToString(),
            DeviceId = deviceId,
            Os = clientInfo.OS.ToString(),
            Browser = clientInfo.UA.ToString(),
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            IsRevoked = false
        };
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