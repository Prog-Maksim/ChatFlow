using System.Net.WebSockets;
using ChatFlow.Models.DB;
using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Other;
using ChatUser = ChatFlow.Models.DB.ChatUser;

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

    /// <summary>
    /// Отправляет сообщение об очистке истории чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="persons">Список пользователей</param>
    /// <returns></returns>
    public Task SendMessageDeleteHistoryChat(string chatId, List<ChatUser> persons);

    /// <summary>
    /// Отправляет сообщение о прочтении сообщения
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="person">Объект пользователя</param>
    /// <returns></returns>
    public Task SendMessageViewMessage(string chatId, string messageId, ChatUser person);
    
    /// <summary>
    /// Отправляет сообщение о том что чат был удален
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="person">Объект пользователя</param>
    /// <returns></returns>
    public Task SendMessageDeleteChat(string chatId, List<ChatUser> persons);
}