using System.Text.Json.Serialization;

namespace ChatFlow.Models.Requests;

public class Message
{
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
    /// Key = идентификатор устройства, Value = зашифрованный симметричный ключ
    /// </summary>
    [JsonPropertyName("keys")]
    public Dictionary<string, string>? Keys { get; set; }
}