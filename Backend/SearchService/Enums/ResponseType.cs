namespace SearchService.Enums;

public enum ResponseType
{
    /// <summary>
    /// Успешно
    /// </summary>
    Ok,
    
    /// <summary>
    /// Не удается проверить корректность jwt токена
    /// </summary>
    JwtTokenVerificationFailed,
    
    /// <summary>
    /// ПОиск ничего не дал
    /// </summary>
    SearchNotFound,
}