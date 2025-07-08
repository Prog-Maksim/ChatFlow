using System.Text.Json.Serialization;
using WebSocketService.Enums;

namespace WebSocketService.Models.Other;

public class MessageData
{
    public required string MessageId { get; set; }
    public required string ChatId { get; set; }
    public required string Text { get; set; }
    public required string OwnerId { get; set; }
    public required MessageStatus MessageType { get; set; } = MessageStatus.Send;
    public required DateTime Created { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Updated { get; set; }
}