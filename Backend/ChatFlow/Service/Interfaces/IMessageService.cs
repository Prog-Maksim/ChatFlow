using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface IMessageService
{
    /// <summary>
    /// Создает сообщение и отправляет в чат
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="message">Объект сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public Task<BaseResponse<string, SendMessage>> SendMessageAsync(string accessToken, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Создает сообщение и отправляет в чат
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="replyMessageId">Идентификатор отвечаемого сообщения</param>
    /// <param name="message">Объект сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public Task<BaseResponse<string, SendMessage>> SendReplyMessageAsync(string accessToken, string replyMessageId, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Выдает все сообщения чата с пагинацией
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="limit">Кол-во сообщений в выдаче</param>
    /// <param name="offset">Смещение от начала списка</param>
    public Task<BaseResponse<string, MessagesPagination>> GetMessagesChatAsync(string accessToken,
        string chatId, int limit, int offset);

    /// <summary>
    /// Возвращает последнее сообщение чата
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<BaseResponse<string, MessageData>> GetLastMessageAsync(string accessToken, string chatId);

    /// <summary>
    /// Удаляет сообщение
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    public Task<BaseResponse<string, MessageData>> DeleteMessageAsync(string accessToken, string chatId,
        string messageId);

    /// <summary>
    /// Обновляет сообщение
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="messageData">Объект обновляемого сообщения</param>
    /// <returns></returns>
    public Task<BaseResponse<string, MessageData>> UpdateMessageAsync(string accessToken, string chatId,
        string messageId, UpdateMessage messageData);
    
    
    /// <summary>
    /// Отмечает сообщение как прочитанное
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> ReadTheMessage(string accessToken, string chatId, string messageId);
    
    /// <summary>
    /// Выдает информации о прочтении сообщения
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    public Task<BaseResponse<string, List<PersonReadMessage>>> GetTheReadMessage(string accessToken, string chatId, string messageId);
}