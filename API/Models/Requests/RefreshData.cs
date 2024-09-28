namespace API.Models.Requests;

public class RefreshData
{
    public string access_token { get; set; }
    public string refresh_token { get; set; }
}