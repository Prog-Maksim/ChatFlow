using ChatFlow.Models.DB;
using ChatFlow.Models.Other;

namespace ChatFlow.Scripts;

public interface IJwtTokenService
{
    /// <summary>
    /// Создает access токен
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="id">Идентификатор</param>
    /// <returns></returns>
    public string GenerateJwtAccessToken(string personId, string sessionId, int id);

    /// <summary>
    /// Создает Refresh токен
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="passwordVersion">Версия пароля</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="id">Идентификатор</param>
    /// <returns></returns>
    public string GenerateJwtRefreshToken(string personId, int passwordVersion, string sessionId, int id);

    /// <summary>
    /// Создает jwt токены для пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="passwordVersion">Версия пароля</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="id">Идентификатор</param>
    /// <returns></returns>
    public Tokens CreateJwtToken(string personId, int passwordVersion, string sessionId, int id);

    /// <summary>
    /// Создает jwt токены для пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="passwordVersion">Версия пароля</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="id">Идентификатор</param>
    /// <param name="oldRefreshToken">Старый refresh токен</param>
    /// <returns></returns>
    public Tokens CreateJwtToken(string personId, int passwordVersion, string sessionId, int id,
        string oldRefreshToken);

    /// <summary>
    /// Возвращает данные токена
    /// </summary>
    /// <param name="token">Токен</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Неверный jwt токен</exception>
    public JwtTokenData GetJwtTokenData(string token);

    /// <summary>
    /// Проверяет валидность токена
    /// </summary>
    /// <param name="token">Данные токена</param>
    /// <param name="person">Объект пользователя</param>
    /// <returns>true - токен валиден</returns>
    public Task<bool> ValidateJwtRefreshToken(JwtTokenData token, Persons person);

    /// <summary>
    /// Проверяет валидность токена
    /// </summary>
    /// <param name="token">Токен</param>
    /// <returns>true - токен валиден</returns>
    public Task<bool> ValidateJwtAccessToken(JwtTokenData token);
}