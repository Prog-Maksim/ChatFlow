namespace API.Models.DB;

public class Person
{
    public int ID { get; set; }
    public string PersonId { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
    public string? Tag { get; set; }
    public string? Description { get; set; }
    public DateTime RegistrationTime { get; set; }
    public string Country { get; set; }
}