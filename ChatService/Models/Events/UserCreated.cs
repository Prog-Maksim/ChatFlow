namespace ChatService.Models.Events;

public class UserCreated
{
    public required string PersonId { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public string? Tag { get; set; }
}