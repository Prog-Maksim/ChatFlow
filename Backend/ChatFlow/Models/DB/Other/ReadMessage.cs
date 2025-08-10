namespace ChatFlow.Models.DB.Other;

public class ReadMessage
{
    public required string PersonId { get; set; }
    public required DateTime TimeStamp { get; set; }
}