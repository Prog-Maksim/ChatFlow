using System.Text.Json.Serialization;
using ChatService.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatService.Models.DB;

public class ChatDocument
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    /// <summary>
    /// Идентификатор чата
    /// </summary>
    public required string ChatId { get; set; }
    
    /// <summary>
    /// Тип чата
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public ChatType Type { get; set; }
    
    /// <summary>
    /// Дата и время создания чата
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Заголовок чата
    /// </summary>
    [BsonIgnoreIfNull]
    public string? Title { get; set; }

    /// <summary>
    /// Описание чата
    /// </summary>
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    /// <summary>
    /// Id фотографии чата
    /// </summary>
    [BsonIgnoreIfNull]
    public string? PhotoId { get; set; }

    /// <summary>
    /// Список пользователей
    /// </summary>
    public List<ChatUser> Persons { get; set; } = new();

    /// <summary>
    /// Токен бота
    /// </summary>
    [BsonIgnoreIfNull]
    public string? BotToken { get; set; }
}

public class ChatUser
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Роль пользователя
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public Roles Role { get; set; } = Roles.User;
}