using ChatFlow.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatFlow.Models.DB;

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
    
    /// <summary>
    /// Дата, когда пользователь присоединился к чату
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}