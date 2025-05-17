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
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="personData">Номер телефона</param>
    /// <returns>Код для доступа к данным</returns>
    public Task<string> GenerateCodeAndSaveAsync(string personId, Person personData);

    /// <summary>
    /// Сохраняет данные для двухфакторной аутентификации с кодом авторизации
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="personData">Номер телефона</param>
    /// <param name="totpCode">Код авторизации</param>
    /// <returns>Код для доступа к данным</returns>
    public Task<string> GenerateCodeAndSaveAsync(string personId, Person personData, string totpCode);
    
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
    /// Проверка токена на блокировку
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="token">Токен</param>
    /// <returns></returns>
    public Task<bool> IsBannedTokenAsync(string personId, string token);
    
    /// <summary>
    /// Сохраняет данные в БД
    /// </summary>
    /// <returns></returns>
    public Task SaveChangesAsync();
}