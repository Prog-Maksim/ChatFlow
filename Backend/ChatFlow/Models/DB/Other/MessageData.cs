using System.Text.Json.Serialization;
using ChatFlow.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatFlow.Models.DB.Other;

public class MessageData: ICloneable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    public required string MessageId { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReplyMessageId { get; set; }
    
    public required string ChatId { get; set; }
    public required string Text { get; set; }
    public required string OwnerId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public required MessageStatus MessageType { get; set; } = MessageStatus.Send;
    public required DateTime Created { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Updated { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? IV { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? HMAC { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Keys { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public List<ReadMessage> Views {  get; set; } = new ();
    
    public object Clone()
    {
        return this.MemberwiseClone();
    }
}