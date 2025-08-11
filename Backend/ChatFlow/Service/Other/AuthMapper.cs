using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Scripts;
using UAParser;
using Sessions = ChatFlow.Models.DB.Sessions;

namespace ChatFlow.Service.Other;

public class AuthMapper
{
    /// <summary>
    /// Создает объект пользователя
    /// </summary>
    /// <param name="registrationUser">Данные пользователя</param>
    /// <param name="encryptedIp">Зашифрованный IP адрес</param>
    /// <param name="passwordHash">Хеш пароля</param>
    /// <returns></returns>
    public static Persons CreateUserEntity(RegistrationUser registrationUser, string encryptedIp, string passwordHash)
    {
        var user = new Persons
        {
            PersonId = Guid.NewGuid().ToString(),
            NumberPhone = registrationUser.Login,
            PasswordVersion = 1,
            RegistrationIp = encryptedIp,
            AccountState = AccountState.Active,
            RegistrationTime = DateTime.UtcNow,
            PasswordHash = passwordHash
        };
        return user;
    }
    
    /// <summary>
    /// Создает объект пользователя
    /// </summary>
    /// <param name="registrationUser">Данные пользователя</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <returns></returns>
    public static DataPersons AddUserDataEntity(RegistrationUser registrationUser, string userId)
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
    
    /// <summary>
    /// Добавляет пользователя в сервис поиска
    /// </summary>
    /// <param name="data">Данные пользователя</param>
    public static UserCreated AddUserToSearch(DataPersons data)
    {
        UserCreated user = new UserCreated
        {
            Name = data.Name,
            Surname = data.Surname,
            Tag = data.Tag,
            PersonId = data.PersonId
        };
        return user;
    }

    /// <summary>
    /// Собирает об]ект AuthTokens
    /// </summary>
    /// <param name="tokens">Токены</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="session">Объект сессии</param>
    /// <returns></returns>
    public static AuthTokens GenerateToken(Tokens tokens, string personId, Sessions session)
    {
        var tokenResult = new AuthTokens
        {
            PersonId = personId,
            DeviceId = session.DeviceId,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute)
        };
        return tokenResult;
    }
    
    /// <summary>
    /// Создает объект сессии
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="encryptedIp">IP адрес</param>
    /// <param name="deviceId">Идентификатор устройства</param>
    /// <param name="userAgent">User агенты пользователя</param>
    /// <returns></returns>
    public static Sessions CreateSession(string personId, string encryptedIp, string deviceId, string userAgent)
    {
        var parser = Parser.GetDefault();
        var clientInfo = parser.Parse(userAgent);
    
        return new Sessions
        {
            PersonId = personId,
            SessionId = Guid.NewGuid().ToString(),
            IpAddress = encryptedIp,
            Device = clientInfo.Device.ToString(),
            DeviceId = deviceId,
            Os = clientInfo.OS.ToString(),
            Browser = clientInfo.UA.ToString(),
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            IsRevoked = false
        };
    }
}