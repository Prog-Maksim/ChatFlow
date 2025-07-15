namespace ChatFlow.Enums;

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
    /// Номер телефона не валиден
    /// </summary>
    PhoneNumberNotValid,
    
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
    
    /// <summary>
    /// Отказано в доступе
    /// </summary>
    AccessDenied,
    
    /// <summary>
    /// Достигнуто максимальное кол-во устройств
    /// </summary>
    DeviceLimitReached,
    
    /// <summary>
    /// Сессии не найдены 
    /// </summary>
    SessionNotFound,
    
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
    /// Соединение не является открытым
    /// </summary>
    WebSocketIsNotOpen,
    
    /// <summary>
    /// Поиск ничего не дал
    /// </summary>
    SearchNotFound
}