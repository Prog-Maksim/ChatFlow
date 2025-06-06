using MessageService.Models.Events;
using MessageService.Repository.Interfaces;
using MongoDB.Driver;

namespace MessageService.Repository;

public class MessageRepository: IMessageRepository
{
    private readonly ILogger<MessageRepository> _logger;
    private readonly IMongoCollection<ChatCreated> _chatCollections;

    public MessageRepository(IMongoClient client, ILogger<MessageRepository> logger)
    {
        _logger = logger;
        
        var database = client.GetDatabase("Chats");
        _chatCollections = database.GetCollection<ChatCreated>("chats");
    }
    
    public async Task AddNewChatAsync(ChatCreated @event)
    {
        await _chatCollections.InsertOneAsync(@event);
    }
}