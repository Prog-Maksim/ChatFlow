using ChatFlow.Models.DB;

namespace ChatFlow.Models.Other;

public class TotpData
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Данные пользователя
    /// </summary>
    public required Persons PersonData { get; set; }
    
    /// <summary>
    /// Totp код
    /// </summary>
    public string? TotpCode { get; set; }
    
    /// <summary>
    /// IP адрес пользователя
    /// </summary>
    public required string IpAddress { get; set; }
    
    /// <summary>
    /// Для чтения?
    /// </summary>
    public required bool IsRead { get; set; }
    
    /// <summary>
    /// Может изменяться?
    /// </summary>
    public required bool IsUpdate {get; set;}
}