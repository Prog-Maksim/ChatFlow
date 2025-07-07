using ChatService.Enums;
using ChatService.Models.DB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatService.Models.Events;

public class ChatCreated
{
    public required string ChatId { get; set; }
    
    public ChatType Type { get; set; }
    public required List<ChatUser> Users { get; set; }
}