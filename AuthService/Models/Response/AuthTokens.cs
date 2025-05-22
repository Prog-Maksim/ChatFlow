using System.Text.Json.Serialization;

namespace AuthService.Models.Response;

public class AuthTokens
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    
    [JsonPropertyName("access-expires-at")]
    public required DateTime AccessTokenExpiration { get; set; }
    
    [JsonPropertyName("refresh-expires-at")]
    public required DateTime RefreshTokenExpiration { get; set; }
}