using Newtonsoft.Json.Linq;

namespace API.Security;

public class DeterminingIPAddress
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
    
    public static async Task<bool> IsUserFromRussia(string ipAddress)
    {
        if (ipAddress == "::1" || ipAddress == "127.0.0.1")
            return true;
        
        var json = await Request(ipAddress);
        var countryCode = json["countryCode"]?.ToString();
        
        return countryCode == "RU";
    }

    public static async Task<string> GetPositionUser(string ipAddress)
    {
        if (ipAddress == "::1" || ipAddress == "127.0.0.1")
            return "Rostov Oblast, Russia";
        
        var json = await Request(ipAddress);

        var region = json["regionName"]?.ToString();
        var country = json["country"]?.ToString();
        
        return $"{region}, {country}";
    }
}