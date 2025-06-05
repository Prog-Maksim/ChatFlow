using System.Net.WebSockets;

namespace WebSocketService.Repository.Interfaces;

public interface IWebSocketConnectionManager
{
    /// <summary>
    /// Добавление подключения пользователя
    /// </summary>
    /// <param name="personId">идентификатор пользователя</param>
    /// <param name="sessionId">идентификатор сессии</param>
    /// <param name="socket">Подключение пользователя</param>
    /// <returns></returns>
    void AddConnection(string personId, string sessionId, WebSocket socket);
    
    /// <summary>
    /// Удаления подключения пользователя
    /// </summary>
    /// <param name="personId">идентификатор пользователя</param>
    /// <param name="sessionId">идентификатор сессии</param>
    /// <returns></returns>
    Task RemoveConnection(string personId, string sessionId);
    
    /// <summary>
    /// Возвращает подключение пользователя
    /// </summary>
    /// <param name="personId">идентификатор пользователя</param>
    /// <param name="sessionId">идентификатор сессии</param>
    /// <returns></returns>
    WebSocket? GetConnectionById(string personId, string sessionId);
}