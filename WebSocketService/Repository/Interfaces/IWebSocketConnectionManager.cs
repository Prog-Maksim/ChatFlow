using System.Net.WebSockets;
using WebSocketService.Models.Other;

namespace WebSocketService.Repository.Interfaces;

public interface IWebSocketConnectionManager
{
    /// <summary>
    /// Добавление подключения пользователя
    /// </summary>
    /// <param name="region">Адрес пользователя</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="socket">Подключение пользователя</param>
    /// <returns></returns>
    void AddConnection(PersonRegion region, string personId, string sessionId, WebSocket socket);

    /// <summary>
    /// Удаления подключения пользователя
    /// </summary>
    /// <param name="region">Адрес пользователя</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    Task RemoveConnection(PersonRegion region, string personId, string sessionId);
    
    /// <summary>
    /// Удаления подключения пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    Task RemoveConnection(string personId, string sessionId);
    
    /// <summary>
    /// Возвращает подключение пользователя
    /// </summary>
    /// <param name="personId">идентификатор пользователя</param>
    /// <param name="sessionId">идентификатор сессии</param>
    /// <returns></returns>
    WebSocket? GetConnectionById(string personId, string sessionId);

    /// <summary>
    /// Отправляет сообщение пользователю
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="message">Объект сообщения</param>
    /// <returns></returns>
    public Task SendMessageToUserAsync(string personId, MessageData message);
}