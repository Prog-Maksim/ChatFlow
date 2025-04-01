using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AuthService.Models.Response;

/// <summary>
/// Базовый класс о результате ответа
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Описание ответа
    /// </summary>
    public required string Message { get; set; }
    
    /// <summary>
    /// Статус выполнения
    /// </summary>
    public required bool Success { get; set; }
    
    /// <summary>
    /// Статус код ответа
    /// </summary>
    public required int StatusCode { get; set; }
    
    /// <summary>
    /// Название ошибки
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}