using AuthService.Enums;

namespace AuthService.Models.Other;

public class JwtTokenData
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Тип токена
    /// </summary>
    public required TokenType TokenType { get; set; }
    
    /// <summary>
    /// Версия пароля
    /// </summary>
    public required int PasswordVersion { get; set; }
    
    /// <summary>
    /// Идентификатор токена
    /// </summary>
    public required string Jti { get; set; }
    
    /// <summary>
    /// Сам токен
    /// </summary>
    public required string Token { get; set; }
}