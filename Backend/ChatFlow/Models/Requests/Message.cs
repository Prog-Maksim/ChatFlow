using System.Text.Json.Serialization;

namespace ChatFlow.Models.Requests;

public class Message
{
    /// <summary>
    /// Идентификатор чата
    /// </summary>
    [JsonPropertyName("chatId")]
    public required string ChatId { get; set; }
    
    /// <summary>
    /// Текст сообщения
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; }
    
    /// <summary>
    /// Подпись сообщения
    /// </summary>
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
    
    /// <summary>
    /// Ключи шифрования
    /// </summary>
    [JsonPropertyName("keys")]
    public Dictionary<string, string>? Keys { get; set; }
}