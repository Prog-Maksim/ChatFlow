using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ChatFlow.Enums;

namespace ChatFlow.Models.DB;

public class Persons
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
    /// Номер телефона пользователя
    /// </summary>
    [StringLength(15)]
    public required string NumberPhone { get; set; }

    /// <summary>
    /// Почта пользователя
    /// </summary>
    [StringLength(100)]
    public string? Email { get; set; }
    
    /// <summary>
    /// Хеш пароля
    /// </summary>
    [StringLength(1000)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Версия пароля
    /// </summary>
    public int PasswordVersion { get; set; }
    
    /// <summary>
    /// TOTP код
    /// </summary>
    [StringLength(100)]
    public string? TotpCode { get; set; }
    
    /// <summary>
    /// Ip адрес регистрации пользователя
    /// </summary>
    [StringLength(50)]
    public required string RegistrationIp { get; set; }

    /// <summary>
    /// Статус аккаунта
    /// </summary>
    public required AccountState AccountState { get; set; }
    
    /// <summary>
    /// Дата и время создания аккаунта
    /// </summary>
    public required DateTime RegistrationTime { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public DataPersons? DataPersons { get; set; }
}
