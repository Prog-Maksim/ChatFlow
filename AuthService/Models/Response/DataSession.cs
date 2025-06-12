using System.Text.Json.Serialization;

namespace AuthService.Models.Response;

public class DataSession
{
    public int Count { get; set; }
    public List<Sessions>? Sessions { get; set; }
}