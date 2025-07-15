using System.Text.Json.Serialization;
using ChatFlow.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatFlow.Models.DB;

public class MessageData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
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