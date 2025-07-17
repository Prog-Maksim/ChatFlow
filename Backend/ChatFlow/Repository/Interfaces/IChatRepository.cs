using ChatFlow.Models.DB;

namespace ChatFlow.Repository.Interfaces;

public interface IChatRepository
{
    /// <summary>
    /// Возвращает статус наличия пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<bool> PersonExistAsync(string personId);
    
    /// <summary>
    /// Возвращает идентификатор личного чата между пользователями
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="otherPersonId">Идентификатор второго пользователя</param>
    /// <returns></returns>
    public Task<string?> GetPrivateChatIdAsync(string personId, string otherPersonId);
    
    /// <summary>
    /// Создание приватного чата
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="otherPersonId">Идентификатор второго пользователя</param>
    /// <returns></returns>
    public Task<ChatDocument> CreatePrivateChatAsync(string personId, string otherPersonId);
    
    /// <summary>
    /// Возвращает все чаты пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<List<ChatDocument>?> GetChats(string personId);
    
    /// <summary>
    /// Возвращает чаты по идентификатору
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public Task<ChatDocument?> GetChat(string chatId);
}