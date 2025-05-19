using System.Text.Json.Serialization;

namespace AuthService.Models.Response;

public class Token2Fa
{
    /// <summary>
    /// Токен для Сервиса двухфакторной аутентификации
    /// </summary>
    [JsonPropertyName("2fa-token")]
    public required string Token { get; set; }
}