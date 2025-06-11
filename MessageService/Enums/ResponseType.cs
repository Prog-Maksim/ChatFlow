namespace MessageService.Enums;

public enum ResponseType
{
    /// <summary>
    /// Успешно
    /// </summary>
    Ok,
    
    /// <summary>
    /// Чат не найден
    /// </summary>
    ChatNotFound,
    
    /// <summary>
    /// Пользователь не состоит в этом чате 
    /// </summary>
    UserNotInChat,
    
    /// <summary>
    /// Сообщения не найдены
    /// </summary>
    MessageNotFound,
    
    /// <summary>
    /// Сообщение не обновлено
    /// </summary>
    MessageNotModified,
    
    /// <summary>
    /// Не удается проверить корректность jwt токена
    /// </summary>
    JwtTokenVerificationFailed
}