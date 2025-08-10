using ChatFlow.Enums;

namespace ChatFlow.Models.Other;

public class JwtTokenData
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public string PersonId { get; set; }
    
    /// <summary>
    /// Идентификатор сессии
    /// </summary>
    public string SessionId { get; set; }
    
    /// <summary>
    /// Идентификатор устройства
    /// </summary>
    public required string DeviceId { get; set; } 
    
    /// <summary>
    /// Id токена к сессии
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Тип токена
    /// </summary>
    public TokenType TokenType { get; set; }
    
    /// <summary>
    /// Версия пароля
    /// </summary>
    public int PasswordVersion { get; set; }
    
    /// <summary>
    /// Идентификатор токена
    /// </summary>
    public string Jti { get; set; }
    
    /// <summary>
    /// Сам токен
    /// </summary>
    public string Token { get; set; }
}