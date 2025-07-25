using ChatFlow.Models.Other;

namespace ChatFlow.Models.Other;

public class ChatCreated
{
    public required string ChatId { get; set; }
    public required List<ChatUser> Users { get; set; }
}