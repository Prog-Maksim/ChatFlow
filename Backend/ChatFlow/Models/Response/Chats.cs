using System.Text.Json.Serialization;

namespace ChatFlow.Models.Response;

public class Chats
{
    public int Count { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PrivateChat>? PrivateChats { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SecretChat>? SecretChats { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GroupChat>? GroupChats { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ChannelChat>? ChannelChats { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Bots>? BotChats { get; set; }
}

public class PrivateChat
{
    public required string ChatId { get; set; }
    public required string TargetPersonId { get; set; }
}

public class SecretChat
{
    public required string ChatId { get; set; }
    public required string TargetPersonId { get; set; }
}

public class BaseChat
{
    public string? ChatId { get; set; }
    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
}

public class GroupChat : BaseChat { }
public class ChannelChat : BaseChat { }
public class Bots : BaseChat { }