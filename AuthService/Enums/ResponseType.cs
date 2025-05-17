namespace AuthService.Enums;

public enum ResponseType
{
    /// <summary>
    /// Успешно
    /// </summary>
    Ok,
    
    /// <summary>
    ///  Не удалось определить ip адрес
    /// </summary>
    IpAddressResolutionFailed,
    
    /// <summary>
    /// Данный номер телефона занят
    /// </summary>
    PhoneNumberInUse,
    
    /// <summary>
    /// Слишком много запросов
    /// </summary>
    TooManyRequests,
    
    /// <summary>
    /// Пользователь не найден
    /// </summary>
    PersonNotFound,
    
    /// <summary>
    /// Пользователь не найден или был заблокирован
    /// </summary>
    PersonNotFoundOrBlocked,
    
    /// <summary>
    /// Сначала нужно завершить регистрацию аккаунта
    /// </summary>
    AccountRegistrationIncomplete,
    
    /// <summary>
    /// Аккаунт заблокирован
    /// </summary>
    AccountIsBlocked,
    
    /// <summary>
    /// Пароль не верен
    /// </summary>
    InvalidPasswordError,
    
    /// <summary>
    /// Вход по электронной почте не поддерживается
    /// </summary>
    EmailLoginNotSupported,
    
    /// <summary>
    /// Некорректная строка
    /// </summary>
    InvalidString,
    
    /// <summary>
    /// Код уже был создан
    /// </summary>
    CodeIsCreated,
    
    /// <summary>
    /// Код не найден
    /// </summary>
    CodeNotFount,
    
    /// <summary>
    /// Код не верен
    /// </summary>
    CodeIsNotValid,
    
    /// <summary>
    /// Сервис не подключен
    /// </summary>
    ServiceNotConnected,
    
    /// <summary>
    /// Подключаемый сервис не найден
    /// </summary>
    ServiceConnectedNotFound,
    
    /// <summary>
    /// Не удается проверить корректность jwt токена
    /// </summary>
    JwtTokenVerificationFailed,
}