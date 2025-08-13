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
    
    /// <summary>
    /// Идентификатор сообщения
    /// </summary>
    public required string MessageId { get; set; }
    
    /// <summary>
    /// ID отвечаемого сообщения
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReplyMessageId { get; set; }
    
    /// <summary>
    /// Данные оригинального сообщения при пересылки
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ForwardInfo? ForwardedFrom { get; set; }
    
    /// <summary>
    /// Идентификатор чата
    /// </summary>
    public required string ChatId { get; set; }
    
    /// <summary>
    /// Текст сообщения
    /// </summary>
    public required string Text { get; set; }
    
    /// <summary>
    /// Идентификатор автора сообщения
    /// </summary>
    public required string OwnerId { get; set; }
    
    /// <summary>
    /// Тип сообщения
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public required MessageStatus MessageType { get; set; } = MessageStatus.Send;
    
    /// <summary>
    /// Дава и время создания
    /// </summary>
    public required DateTime Created { get; set; }
    
    /// <summary>
    /// Дата и время обновления
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Updated { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? IV { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? HMAC { get; set; }
    
    /// <summary>
    /// Подпись
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Signature { get; set; }
    
    /// <summary>
    /// Ключи шифрования
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Keys { get; set; }
    
    /// <summary>
    /// Список просмотров сообщения
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public List<ReadMessage> Views {  get; set; } = new ();
    
    public object Clone()
    {
        return MemberwiseClone();
    }
}