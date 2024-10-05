namespace API.Models.DB;

public class PersonChat
{
    public int ID { get; set; }
    public int ChatId { get; set; }
    public string PersonId { get; set; }
    
    public Chat Chat { get; set; }
}