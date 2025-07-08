using System.Text.Json.Serialization;
using MessageService.Enums;
using MessageService.Models.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MessageService.Models.DB;

public class ChatData
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    public required string ChatId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public ChatType Type { get; set; }
    
    public required List<ChatUser> Users { get; set; }

    public static explicit operator ChatData(ChatCreated @event)
    {
        return new ChatData
        {
            ChatId = @event.ChatId,
            Type = @event.Type,
            Users = @event.Users
        };
    }
}