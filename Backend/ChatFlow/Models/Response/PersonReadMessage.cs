namespace ChatFlow.Models.Response;

public class PersonReadMessage
{
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public string? ProfileImageUrl { get; set; }
    public required DateTime TimeStamp { get; set; }
}