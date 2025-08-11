using System.Text.Json.Serialization;
using ChatFlow.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatFlow.Models.DB.Other;

public class ChatDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
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
    /// Время, когда пользователь скрыл чат для себя
    /// Value = PersonId
    /// </summary>
    public List<string> HiddenForUsers { get; set; } = new();

    /// <summary>
    /// Время, когда пользователь удалил все сообщения чата для себя
    /// Key = PersonId, Value = дата удаления сообщений
    /// </summary>
    public Dictionary<string, DateTime> ClearedMessagesForUsers { get; set; } = new();
    
    /// <summary>
    /// Закрепленные сообщения чата
    /// Key = PersonId, Value = список закреплённых сообщений
    /// </summary>
    public Dictionary<string, List<PinnedMessageInfo>> PinnedMessages  { get; set; } = new();
}