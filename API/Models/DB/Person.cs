namespace API.Models.DB;

public class Person
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
    
    public string? Tag { get; set; }
    public string? Description { get; set; }
    public Security? Security { get; set; }
}