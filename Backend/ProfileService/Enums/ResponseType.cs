namespace ProfileService.Enums;

public enum ResponseType
{
    /// <summary>
    /// Успешно
    /// </summary>
    Ok,
    
    /// <summary>
    /// Файл слишком большой
    /// </summary>
    FileTooLarge,
    
    /// <summary>
    /// Неверный файл
    /// </summary>
    InvalidFile,
    
    /// <summary>
    /// Ошибка при загрузке изображения
    /// </summary>
    ErrorUploadFile,
    
    /// <summary>
    /// Допущено предельное кол-во изображений
    /// </summary>
    ImageLimitReached,
    
    /// <summary>
    /// Изображение не найдено
    /// </summary>
    ImageNotFound,
    
    /// <summary>
    /// Данный тег занят
    /// </summary>
    TagAlreadyExists,
    
    /// <summary>
    /// Не удается проверить корректность jwt токена
    /// </summary>
    JwtTokenVerificationFailed
}