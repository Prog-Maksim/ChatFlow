namespace WebSocketService.Enums;

public enum ResponseType
{
    /// <summary>
    /// Успешно
    /// </summary>
    Ok,
    
    /// <summary>
    /// Соединение не является открытым
    /// </summary>
    WebSocketIsNotOpen,
    
    /// <summary>
    /// Не удается проверить корректность jwt токена
    /// </summary>
    JwtTokenVerificationFailed
}