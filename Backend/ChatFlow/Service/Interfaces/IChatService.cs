using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface IChatService
{
    /// <summary>
    /// Создает личный чат
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="otherPersonId">Идентификатор второго пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, CreateChat>> CreatePrivateChat(string accessToken, string otherPersonId);
    
    /// <summary>
    /// Создает секретный чат
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="otherPersonId">Идентификатор второго пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, CreateChat>> CreateSecretPrivateChat(string accessToken, string otherPersonId);

    /// <summary>
    /// Возвращает чаты пользователя
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <returns></returns>
    public Task<BaseResponse<string, Chats>> GetChats(string accessToken);

    /// <summary>
    /// Возвращает подробную информацию о чате
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<BaseResponse<string, ChatInfo>> GetChatInfo(string accessToken, string chatId);

    /// <summary>
    /// Очищает историю чата
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="isAll">Удалить для всех</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> DeleteAllMessages(string accessToken, string chatId, bool isAll = false);

    /// <summary>
    /// Удаляет чат
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="isAll">Удалить для всех</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> DeleteChat(string accessToken, string chatId, bool isAll = false);
    
    /// <summary>
    /// Закрепляет сообщение в чате
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="isAll">Удалить для всех</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> PinnedMessage(string accessToken, string chatId, string messageId, bool isAll = false);
    
    /// <summary>
    /// Открепляет сообщение в чате
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="isAll">Удалить для всех</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> UnPinnedMessage(string accessToken, string chatId, string messageId, bool isAll = false);
    
    /// <summary>
    /// Выдает закрепленные сообщения в чате
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<BaseResponse<string, PinnedMessage>> GetPinnedMessage(string accessToken, string chatId);
}