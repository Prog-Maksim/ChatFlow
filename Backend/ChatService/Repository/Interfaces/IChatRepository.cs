using ChatService.Models.DB;
using ChatService.Models.Events;

namespace ChatService.Repository.Interfaces;

public interface IChatRepository
{
    public Task CreatePersonAsync(UserCreated user);
    
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
}