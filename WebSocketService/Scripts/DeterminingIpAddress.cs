using WebSocketService.Models.Other;
using Newtonsoft.Json.Linq;

namespace WebSocketService.Scripts;

public static class DeterminingIpAddress
{
    private static async Task<JObject> Request(string ipAddress)
    {
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetStringAsync($"http://ip-api.com/json/{ipAddress}");
            var json = JObject.Parse(response);
            return json;
        }
    }
    
    public static async Task<PersonRegion> GetPositionUser(string ipAddress)
    {
        if (ipAddress == "::1" || ipAddress == "127.0.0.1")
            return new PersonRegion { Country = "Russia", City = "Rostov Oblast", Latitude = "47.2359", Longitude = "39.7134" };
        
        var json = await Request(ipAddress);

        var region = json["regionName"]?.ToString();
        var country = json["country"]?.ToString();
        var latitude = json["lat"]?.ToString();
        var longitude = json["lon"]?.ToString();
        
        return new PersonRegion { Country = country, City = region, Latitude = latitude, Longitude = longitude };
    }
}