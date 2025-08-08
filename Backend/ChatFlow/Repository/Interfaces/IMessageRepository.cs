using ChatFlow.Models.DB;
using ChatFlow.Models.DB.Other;

namespace ChatFlow.Repository.Interfaces;

public interface IMessageRepository
{
    /// <summary>
    /// Возвращает данные чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<ChatDocument?> GetChatAsync(string chatId);

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

    /// <summary>
    /// Удаление сообщения в чате
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    public Task<bool> DeleteMessageAsync(string chatId, string messageId);
    
    /// <summary>
    /// Удаление всех сообщений в чате 
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<bool> DeleteAllMessageAsync(string chatId);
}