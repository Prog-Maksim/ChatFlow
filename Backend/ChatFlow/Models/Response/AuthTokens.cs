using System.Text.Json.Serialization;

namespace ChatFlow.Models.Response;

public class AuthTokens
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Access токен
    /// </summary>
    public required string AccessToken { get; set; }
    
    /// <summary>
    /// Refresh токен
    /// </summary>
    public required string RefreshToken { get; set; }
    
    /// <summary>
    /// До которого времени токен валиден
    /// </summary>
    [JsonPropertyName("access-expires-at")]
    public required DateTime AccessTokenExpiration { get; set; }
    
    /// <summary>
    /// До которого времени токен валиден
    /// </summary>
    [JsonPropertyName("refresh-expires-at")]
    public required DateTime RefreshTokenExpiration { get; set; }
}