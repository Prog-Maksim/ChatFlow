using MessageService.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MessageService.Models.Events;

public class ChatCreated
{
    public required string ChatId { get; set; }
    public ChatType Type { get; set; }
    public required List<ChatUser> Users { get; set; }
}