namespace API.Models.DB;

public class Session
{
    public int ID { get; set; }
    public string PersonId { get; set; }
    public string SessionId { get; set; }
    public string LoginDevice { get; set; }
    public string IpAdress { get; set; }
    public DateTime LoginTime { get; set; }
    public string LoginCountry { get; set; }
	public string SessionToken { get; set; }
}