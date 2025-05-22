using System.Text.Json.Serialization;

namespace AuthService.Models.Response;

public class RegistrationCode
{
    /// <summary>
    /// Код для подключения двухфакторной аутентификации
    /// </summary>
    public required string Code { get; set; }
    
    /// <summary>
    /// До какого времени годен токен
    /// </summary>
    [JsonPropertyName("expires-at")]
    public required DateTime ExpiresAt { get; set; }
}