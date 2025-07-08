using AuthService.Models.Other;
using Newtonsoft.Json.Linq;

namespace AuthService.Scripts;

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
            return new PersonRegion { Country = "Russia", City = "Rostov Oblast" };
        
        var json = await Request(ipAddress);

        var region = json["regionName"]?.ToString();
        var country = json["country"]?.ToString();
        
        return new PersonRegion { Country = country, City = region };
    }
}