using System.Text.Json.Serialization;

namespace ChatFlow.Models.Response;

public class RegistrationCode
{
    /// <summary>
    /// Код для подключения двухфакторной аутентификации
    /// </summary>
    public required string Code { get; set; }
    
    /// <summary>
    /// До какого времени токен валиден
    /// </summary>
    [JsonPropertyName("expires-at")]
    public required DateTime ExpiresAt { get; set; }
}