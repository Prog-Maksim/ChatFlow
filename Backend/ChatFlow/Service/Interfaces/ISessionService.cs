using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface ISessionService
{
    /// <summary>
    /// Выдает все активные сессии
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <returns></returns>
    public Task<BaseResponse<string, DataSession>> GetSessions(string accessToken);

    /// <summary>
    /// Отзывает сессии
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    public Task<BaseResponse<string, List<RevokeSession>>> RevokeSession(string accessToken, string? sessionId = null);

    /// <summary>
    /// Блокирует подключение пользователя
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="personId">Идентификатор пользователя</param>
    public Task CloseConnection(string sessionId, string personId);

    /// <summary>
    /// Блокирует подключение пользователя
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="personId">Идентификатор пользователя</param>
    public Task CloseConnection(List<string> sessionId, string personId);
}