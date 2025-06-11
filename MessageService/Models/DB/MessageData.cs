using System.Text.Json.Serialization;
using MessageService.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MessageService.Models.DB;

public class MessageData
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    public required string MessageId { get; set; }
    public required string ChatId { get; set; }
    public required string Text { get; set; }
    public required string OwnerId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public required MessageStatus MessageType { get; set; } = MessageStatus.Send;
    public required DateTime Created { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Updated { get; set; }
}