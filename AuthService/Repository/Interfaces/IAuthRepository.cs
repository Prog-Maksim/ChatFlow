using AuthService.Models.DB;
using AuthService.Models.Other;

namespace AuthService.Repository.Interfaces;

public interface IAuthRepository
{
    /// <summary>
    /// Выдает данные пользователя по номеру телефона
    /// </summary>
    /// <param name="phoneNumber">Номер телефона</param>
    /// <returns></returns>
    public Task<Person?> GetUserByPhoneNumberAsync(string phoneNumber);
    
    /// <summary>
    /// Выдает данные пользователя по ID
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<Person?> GetUserByIdAsync(string personId);
    
    /// <summary>
    /// Добавляет нового пользователя
    /// </summary>
    /// <param name="person">Данные пользователя</param>
    /// <returns></returns>
    public Task<bool> AddUserAsync(Person person);
    
    /// <summary>
    /// Обновление данных пользователя
    /// </summary>
    /// <param name="person">Данные пользователя</param>
    /// <returns></returns>
    public bool UpdateUserAsync(Person person);

    /// <summary>
    /// Сохраняет данные для двухфакторной аутентификации
    /// </summary>
    /// <param name="personData">Данные пользователя</param>
    /// <param name="userIpAdress">IP адрес пользователя</param>
    /// <returns>Код для доступа к данным</returns>
    public Task<string> GenerateCodeAndSaveAsync(Person personData, string userIpAdress);

    /// <summary>
    /// Сохраняет данные для двухфакторной аутентификации с кодом авторизации
    /// </summary>
    /// <param name="personData">Данные пользователя</param>
    /// <param name="userIpAdress">IP адрес пользователя</param>
    /// <param name="totpCode">Код авторизации</param>
    /// <returns>Код для доступа к данным</returns>
    public Task<string> GenerateCodeAndSaveAsync(Person personData, string userIpAdress, string totpCode);
    
    /// <summary>
    /// Проверяет наличие кода в БД
    /// </summary>
    /// <param name="code">Код с данными</param>
    /// <returns>Результат поиска</returns>
    public Task<bool> CheckCodeAsync(string code);
    
    /// <summary>
    /// Возвращает данные по коду
    /// </summary>
    /// <param name="code">Код с данными</param>
    /// <returns>Денные по коду</returns>
    public Task<TotpData?> GetTotpDataByCodeAsync(string code);
    
    /// <summary>
    /// Обновление TOTP кода в БД
    /// </summary>
    /// <param name="code">Код с данными</param>
    /// <param name="totpCode">Новый TOTP код</param>
    /// <returns>Результат обновления</returns>
    public Task<bool> UpdateTotpDataByCodeAsync(string code, string totpCode);
    
    /// <summary>
    /// Удаляет данные по коду
    /// </summary>
    /// <param name="code">Код с данными</param>
    /// <returns></returns>
    public Task DeleteTotpDataByCodeAsync(string code);

    /// <summary>
    /// Блокировка списка токенов
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="tokens">Список токенов</param>
    /// <returns></returns>
    public Task AddJwtTokensToBanAsync(string personId, List<string> tokens);
    
    /// <summary>
    /// Блокировка токена
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="token">Токен</param>
    /// <returns></returns>
    public Task AddJwtTokenToBanAsync(string personId, string token);
    
    /// <summary>
    /// Блокировка сессии
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    public Task AddSessionToBanAsync(string sessionId);
    
    /// <summary>
    /// Добавляет в бан список сессий
    /// </summary>
    /// <param name="sessionIds"></param>
    /// <returns></returns>
    public Task AddSessionsToBanAsync(IEnumerable<string> sessionIds);

    /// <summary>
    /// Проверка токена на блокировку
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="token">Токен</param>
    /// <returns>true - токен не валиден</returns>
    public Task<bool> IsBannedTokenAsync(string personId, string token, string sessionId);

    /// <summary>
    /// Проверка не заблокирован ли ip адрес за частый перебор пароля
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public Task<bool> IsBlockedAsync(string ip);

    /// <summary>
    ///  Увеличиваем счетчик неправильных попыток ввода пароля
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public Task IncrementLoginAttemptsAsync(string ip);
    
    /// <summary>
    /// Сохраняет данные в БД
    /// </summary>
    /// <returns></returns>
    public Task SaveChangesAsync();
    
    /// <summary>
    /// Возвращает кол-во сессий для пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<int> GetNumberSessionsAsync(string personId);
    
    /// <summary>
    /// Добавляет новую сессию для пользователя
    /// </summary>
    /// <param name="session">Обьект сессии</param>
    /// <returns></returns>
    public Task<bool> AddSessionAsync(Session session);

    /// <summary>
    /// Возвращает объект сессии
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    public Task<Session?> GetSessionByIdAsync(string personId, string sessionId);

    /// <summary>
    /// Возвразает все сессии для пользователя 
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="state">Состояние сессии</param>
    /// <returns></returns>
    public Task<IQueryable<Session>?> GetSessionsAsync(string personId, bool state = false);
    
    public Task RevokeAllSessionsAsync(string personId);
}