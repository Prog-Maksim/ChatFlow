namespace ChatFlow.Models.Requests;

public class UpdateMessage
{
    public required string Text { get; set; }
    
    public string? Signature { get; set; }
    public Dictionary<string, string>? Keys { get; set; }
}