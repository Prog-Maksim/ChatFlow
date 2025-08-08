using System.Net.WebSockets;
using ChatFlow.Models.DB;
using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Other;

namespace ChatFlow.Repository.Interfaces;

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
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    WebSocket? GetConnectionById(string personId, string sessionId);

    /// <summary>
    /// Отправляет сообщение пользователю
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="message">Объект сообщения</param>
    /// <returns></returns>
    public Task SendMessageToUserAsync(string personId, MessageData message);

    /// <summary>
    /// Отправляет сообщение о том что был создан чат
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task SendMessageCreateChat(string chatId, string personId);

    /// <summary>
    /// Оповещает пользователей о миграции чата
    /// </summary>
    /// <param name="oldChatId">Старый идентификатор чата</param>
    /// <param name="newChatId">Новый идентификатор чата</param>
    /// <param name="chatData">Данные чата</param>
    /// <returns></returns>
    public Task SendMessageMigrationChat(string oldChatId, string newChatId, ChatDocument chatData);
}