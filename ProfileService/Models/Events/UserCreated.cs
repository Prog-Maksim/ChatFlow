namespace ProfileService.Models.Events;

public class UserCreated
{
    public required string PersonId { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
}