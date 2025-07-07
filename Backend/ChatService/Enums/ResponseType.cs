namespace ChatService.Enums;

public enum ResponseType
{
    /// <summary>
    /// Успешно
    /// </summary>
    Ok,
    
    /// <summary>
    /// Пользователь не найден
    /// </summary>
    PersonNotFound,
    
    /// <summary>
    /// Не удается проверить корректность jwt токена
    /// </summary>
    JwtTokenVerificationFailed
}