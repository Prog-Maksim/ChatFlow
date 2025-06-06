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
}