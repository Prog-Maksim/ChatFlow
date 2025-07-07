using MessageService.Models.DB;
using MessageService.Models.Events;

namespace MessageService.Repository.Interfaces;

public interface IMessageRepository
{
    /// <summary>
    /// Сообщение о создании нового чата
    /// </summary>
    /// <param name="event">Данные события</param>
    /// <returns></returns>
    public Task AddNewChatAsync(ChatCreated @event);
    
    /// <summary>
    /// Возвращает данные чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<ChatData?> GetChatAsync(string chatId);

    /// <summary>
    /// Сохраняет объект сообщения в БД
    /// </summary>
    /// <param name="message">Объект сообщения</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SaveMessageAsync(MessageData message, CancellationToken token);

    /// <summary>
    /// Выдает сообщения в чате с пагинацией
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public Task<(List<MessageData> Messages, long TotalCount)> GetMessagesByChatIdAsync(string chatId, int limit, int offset);
    
    /// <summary>
    /// Возвращает последнее сообщение чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<MessageData?> GetLastMessageAsync(string chatId);
    
    /// <summary>
    /// Возвращает данные сообщения
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    public Task<MessageData?> GetMessageByIdAsync(string chatId, string messageId);

    /// <summary>
    /// Обновляет данные сообщения
    /// </summary>
    /// <param name="updatedMessage">Объект обновляемого сообщения</param>
    /// <returns></returns>
    public Task<bool> UpdateMessageAsync(MessageData updatedMessage);
}