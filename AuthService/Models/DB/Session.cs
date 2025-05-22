using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DB;

public class Session
{
    /// <summary>
    /// Идентификатор записи
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [StringLength(36)]
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Идентификатор сессии
    /// </summary>
    [StringLength(36)]
    public required string SessionId { get; set; }
    
    /// <summary>
    /// IP адрес входа
    /// </summary>
    [StringLength(45)]
    public required string IpAddress { get; set; }
    
    /// <summary>
    /// Устройство входа
    /// </summary>
    [StringLength(64)]
    public string? Device { get; set; }
    
    /// <summary>
    /// Операционная система входа
    /// </summary>
    [StringLength(64)]
    public string? Os { get; set; }
    
    /// <summary>
    /// Браузер входа
    /// </summary>
    [StringLength(128)]
    public string? Browser { get; set; }
    
    /// <summary>
    /// Дата и время входа
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Дата и время последнего действия
    /// </summary>
    public DateTime LastUsedAt { get; set; }
    
    /// <summary>
    /// Статус входа (действительный или отозван)
    /// </summary>
    public bool IsRevoked { get; set; }
}