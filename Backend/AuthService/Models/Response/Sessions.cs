using System.Text.Json.Serialization;

namespace AuthService.Models.Response;

public class Sessions
{
    /// <summary>
    /// Ip адрес сессии
    /// </summary>
    public required string IpAddress { get; set; }
    
    /// <summary>
    /// Город сессии
    /// </summary>
    public required string City { get; set; }
    
    /// <summary>
    /// Страна сессии
    /// </summary>
    public required string Country { get; set; }
    
    /// <summary>
    /// Устройство сессии
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Device { get; set; }
    
    /// <summary>
    /// Операционная система сессии
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Os { get; set; }
    
    /// <summary>
    /// Браузер сессии
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Browser { get; set; }
    
    /// <summary>
    /// Дата и время создание сессии
    /// </summary>
    public required DateTime CreateAt { get; set; }
    
    /// <summary>
    /// Дата и время последнего использования сессии
    /// </summary>
    public required DateTime LastUsedAt { get; set; }
    
    /// <summary>
    /// Идентификатор сессии
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Сессия принадлежит этому устройству
    /// </summary>
    public required bool IsYou { get; set; } = false;
}