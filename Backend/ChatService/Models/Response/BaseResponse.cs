using System.Text.Json.Serialization;
using ChatService.Enums;

namespace ChatService.Models.Response;

/// <summary>
/// Базовый класс о результате ответа
/// </summary>
public class BaseResponse<TError, TData>
{
    /// <summary>
    /// Описание ответа
    /// </summary>
    public required string Message { get; set; }
    
    /// <summary>
    /// Статус выполнения
    /// </summary>
    public required bool Successfully { get; set; }
    
    /// <summary>
    /// Статус код ответа
    /// </summary>
    public required int Status { get; set; }
    
    /// <summary>
    /// Тип результата
    /// </summary>
    public required ResponseType Type { get; set; }
    
    /// <summary>
    /// Описание ошибок
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TError? Errors { get; set; }
    
    /// <summary>
    /// Полезная нагрузка
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TData? Data { get; set; }
}