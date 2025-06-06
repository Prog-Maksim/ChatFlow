using ChatService.Enums;
using ChatService.Models.DB;
using ChatService.Models.Events;
using ChatService.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using StackExchange.Redis;

namespace ChatService.Repository;

public class ChatRepository: IChatRepository
{
    private readonly ILogger<ChatRepository> _logger;
    private readonly ApplicationContext _context;
    private readonly IDatabase _database;
    private readonly IMongoCollection<ChatDocument> _chatCollections;

    public ChatRepository(ILogger<ChatRepository> logger, ApplicationContext context, IConnectionMultiplexer connectionMultiplexer, IMongoClient client)
    {
        _logger = logger;
        _context = context;
        _database = connectionMultiplexer.GetDatabase();
        
        var database = client.GetDatabase("ChatData");
        _chatCollections = database.GetCollection<ChatDocument>("chats");
    }

    public async Task CreatePersonAsync(UserCreated user)
    {
        await _context.Persons.AddAsync(new Person
        {
            PersonId = user.PersonId
        });
        await _context.SaveChangesAsync();
    }

    public async Task<bool> PersonExistAsync(string personId)
    {
        var cacheKey = $"user:exists:{personId}";
        
        var cached = await _database.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return cached == "1";
        
        var exists = await _context.Persons.AnyAsync(u => u.PersonId == personId);

        if (exists)
            await _database.StringSetAsync(cacheKey, "1", TimeSpan.FromHours(1));
        else
            await _database.StringSetAsync(cacheKey, "0");

        return exists;
    }

    public async Task<string?> GetPrivateChatIdAsync(string personId, string otherPersonId)
    {
        var filter = Builders<ChatDocument>.Filter.And(
            Builders<ChatDocument>.Filter.Eq(c => c.Type, ChatType.Private),
            Builders<ChatDocument>.Filter.Size(c => c.Persons, 2),
            Builders<ChatDocument>.Filter.ElemMatch(c => c.Persons, p => p.PersonId == personId),
            Builders<ChatDocument>.Filter.ElemMatch(c => c.Persons, p => p.PersonId == otherPersonId)
        );

        var chat = await _chatCollections.Find(filter).FirstOrDefaultAsync();
        return chat?.Id;
    }

    public async Task<ChatDocument> CreatePrivateChatAsync(string personId, string otherPersonId)
    {
        var chat = new ChatDocument
        {
            ChatId = Guid.NewGuid().ToString(),
            Type = ChatType.Private,
            CreatedAt = DateTime.UtcNow,
            Persons = new List<ChatUser>
            {
                new ()
                {
                    PersonId = personId,
                    Role = Roles.Owner
                },
                new ()
                {
                    PersonId = otherPersonId,
                    Role = Roles.User
                }
            }
        };

        await _chatCollections.InsertOneAsync(chat);
        return chat;
    }

    public async Task<List<ChatDocument>?> GetChats(string personId)
    {
        var filter = Builders<ChatDocument>.Filter.And(
            Builders<ChatDocument>.Filter.ElemMatch(c => c.Persons, p => p.PersonId == personId)
        );

        var chat = await _chatCollections.Find(filter).ToListAsync();
        return chat;
    }
}