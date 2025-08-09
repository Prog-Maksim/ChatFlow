using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.DB.Other;
using ChatFlow.Repository.Interfaces;
using MongoDB.Driver;

namespace ChatFlow.Repository;

public class MessageRepository: IMessageRepository
{
    private readonly ILogger<MessageRepository> _logger;
    private readonly IMongoCollection<ChatDocument> _chatCollections;
    private readonly IMongoCollection<MessageData> _messageCollections;

    public MessageRepository(IMongoClient client, ILogger<MessageRepository> logger)
    {
        _logger = logger;
        
        var chatDatabase = client.GetDatabase("ChatData");
        var messageDatabase = client.GetDatabase("Messages");
        
        _chatCollections = chatDatabase.GetCollection<ChatDocument>("chats");
        _messageCollections = messageDatabase.GetCollection<MessageData>("messages");
    }

    public async Task<ChatDocument?> GetChatAsync(string chatId)
    {
        var filter = Builders<ChatDocument>.Filter.Eq(chat => chat.ChatId, chatId);
        return await _chatCollections.Find(filter).FirstOrDefaultAsync();
    }

    public async Task SaveMessageAsync(MessageData message, CancellationToken token)
    {
        await _messageCollections.InsertOneAsync(message,null, token);
    }
    
    public async Task<(List<MessageData> Messages, long TotalCount)> GetMessagesByChatIdAsync(string chatId, int limit, int offset)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(x => x.ChatId, chatId),
            Builders<MessageData>.Filter.Ne(x => x.MessageType, MessageStatus.Deleted)
                );

            var totalCount = await _messageCollections.CountDocumentsAsync(filter);

            var messages = await _messageCollections
                .Find(filter)
                .SortByDescending(x => x.Created)
                .Skip(offset)
                .Limit(limit)
                .ToListAsync();

            return (messages, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сообщений для чата {ChatId}", chatId);
            return (new List<MessageData>(), 0);
        }
    }
    
    public async Task<(List<MessageData> Messages, long TotalCount)> GetMessagesByChatIdAsync(
        string chatId, int limit, int offset, string deviceId)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(x => x.ChatId, chatId),
                Builders<MessageData>.Filter.Ne(x => x.MessageType, MessageStatus.Deleted),
                Builders<MessageData>.Filter.Exists($"Keys.{deviceId}")
            );

            var totalCount = await _messageCollections.CountDocumentsAsync(filter);

            var messages = await _messageCollections
                .Find(filter)
                .SortByDescending(x => x.Created)
                .Skip(offset)
                .Limit(limit)
                .ToListAsync();

            return (messages, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сообщений для чата {ChatId} и устройства {DeviceId}", chatId, deviceId);
            return (new List<MessageData>(), 0);
        }
    }

    public async Task<MessageData?> GetLastMessageAsync(string chatId)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(x => x.ChatId, chatId),
                Builders<MessageData>.Filter.Ne(x => x.MessageType, MessageStatus.Deleted)
            );
            
            var messages = await _messageCollections
                .Find(filter)
                .SortByDescending(x => x.Created)
                .Limit(1).FirstOrDefaultAsync();

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сообщений для чата {ChatId}", chatId);
            return null;
        }
    }

    public async Task<MessageData?> GetLastMessageAsync(string chatId, string deviceId)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(x => x.ChatId, chatId),
                Builders<MessageData>.Filter.Ne(x => x.MessageType, MessageStatus.Deleted),
                Builders<MessageData>.Filter.Exists($"Keys.{deviceId}")
            );

            var message = await _messageCollections
                .Find(filter)
                .SortByDescending(x => x.Created)
                .Limit(1)
                .FirstOrDefaultAsync();

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении последнего сообщения для чата {ChatId} и устройства {DeviceId}", chatId, deviceId);
            return null;
        }
    }

    public async Task<MessageData?> GetMessageByIdAsync(string chatId, string messageId)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(x => x.ChatId, chatId),
                Builders<MessageData>.Filter.Eq(x => x.MessageId, messageId),
                Builders<MessageData>.Filter.Ne(x => x.MessageType, MessageStatus.Deleted)
            );
            
            var message = await _messageCollections
                .Find(filter).FirstOrDefaultAsync();

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сообщений для чата {ChatId}", chatId);
            return null;
        }
    }
    
    public async Task<bool> UpdateMessageAsync(MessageData updatedMessage)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.Eq(m => m.MessageId, updatedMessage.MessageId);

            var update = Builders<MessageData>.Update
                .Set(m => m.Text, updatedMessage.Text)
                .Set(m => m.Updated, DateTime.UtcNow)
                .Set(m => m.MessageType, updatedMessage.MessageType)
                .Set(m => m.IV, updatedMessage.IV)
                .Set(m => m.HMAC, updatedMessage.HMAC)
                .Set(m => m.Signature, updatedMessage.Signature)
                .Set(m => m.Keys,  updatedMessage.Keys);

            var result = await _messageCollections.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении сообщения {MessageId}", updatedMessage.MessageId);
            return false;
        }
    }

    public async Task<bool> DeleteMessageAsync(string chatId, string messageId)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(m => m.ChatId, chatId),
                Builders<MessageData>.Filter.Eq(m => m.MessageId, messageId)
            );

            var result = await _messageCollections.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении сообщения {MessageId} в чате {ChatId}", messageId, chatId);
            return false;
        }
    }

    public async Task<bool> DeleteAllMessageAsync(string chatId)
    {
        try
        {
            var filter = Builders<MessageData>.Filter.Eq(m => m.ChatId, chatId);

            var result = await _messageCollections.DeleteManyAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении всех сообщений в чате {ChatId}", chatId);
            return false;
        }
    }
}