namespace API.Models.DB;

public class Security
{
    public string? SessionId { get; set; }
    public string LoginDevice { get; set; }
    public string UserAgent { get; set; }
    public DateTime LoginTime { get; set; }
    public string? PersonIpAdress { get; set; }
    public string LoginCountry { get; set; }
    public string? Token { get; set; }
}