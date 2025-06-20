using AuthService.Enums;
using AuthService.Models.Response;

namespace AuthService.Scripts;

public static class ResponseFactory
{
    /// <summary>
    /// JWT токен не действительный
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> JwtTokenInvalid<TData>() =>
        new() { Message = "JWT токен недействителен", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false };

    /// <summary>
    /// Пользователь не найден
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> PersonNotFound<TData>() =>
        new() { Message = "Пользователь не найден", Type = ResponseType.PersonNotFound, Errors = "Not Found", Status = 404, Successfully = false };

    /// <summary>
    /// Пользователь не найден или был заблокирован
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, T> PersonNotFoundOrBlocked<T>() =>
        new() { Message = "Пользователь не найден или заблокирован", Type = ResponseType.PersonNotFoundOrBlocked, Errors = "Forbidden", Status = 423, Successfully = false};

    /// <summary>
    /// Отказано в доступе
    /// </summary>
    /// <param name="message"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, T> AccessDenied<T>(string message = "Отказано в доступе") =>
        new() { Message = message, Type = ResponseType.AccessDenied, Errors = "Forbidden", Status = 403, Successfully = false };
    
    /// <summary>
    /// Неверный пароль
    /// </summary>
    /// <param name="message">Подробная информация</param>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> InvalidPassword<TData>(string message) =>
        new() { Message = message, Type = ResponseType.InvalidPasswordError, Errors = "Forbidden", Status = 403, Successfully = false };
    
    /// <summary>
    /// Номер телефона занят
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> PhoneNumberInUse<TData>() =>
        new() { Message = "Данный номер телефона занят", Successfully = false, Status = 403, Type = ResponseType.PhoneNumberInUse, Errors = "Forbidden" };
    
    /// <summary>
    /// Слишком много попыток входа
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> TooManyRequests<TData>() =>
        new() { Status = 429, Message = "Слишком много попыток входа. Попробуйте еще раз позже.", Type = ResponseType.TooManyRequests, Errors = "Too Many Requests", Successfully = false };

    /// <summary>
    /// Успешно
    /// </summary>
    /// <param name="message"></param>
    /// <param name="data"></param>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> Success<TData>(string message, TData data) =>
        new() { Message = message, Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = data };
    
    /// <summary>
    /// Авторизация по Email не поддерживается
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> EmailNotSupported<TData>() =>
        new() { Message = "Вход по электронной почте сейчас не поддерживается", Status = 400, Type = ResponseType.EmailLoginNotSupported, Errors = "Bad Request", Successfully = false };
    
    public static BaseResponse<string, TData> BadRequest<TData>(string message) =>
        new() { Message = message, Status = 400, Type = ResponseType.InvalidString, Errors = "Bad Request", Successfully = false };

    /// <summary>
    /// Аккаунт не активирован
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> AccountNotActivated<TData>() =>
        new() { Status = 403, Message = "Требуется сначала завершить регистрацию аккаунта", Type = ResponseType.AccountRegistrationIncomplete, Errors = "Forbidden", Successfully = false };

    /// <summary>
    /// Аккаунт заблокирован
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> AccountBlocked<TData>() =>
        new() { Status = 403, Message = "Данный аккаунт заблокирован", Type = ResponseType.AccountIsBlocked, Errors = "Forbidden", Successfully = false };
    
    /// <summary>
    /// Сессии не найдены
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static BaseResponse<string, TData> SessionNotFound<TData>() =>
        new() { Message = "Сессии не найдены", Type = ResponseType.SessionNotFound, Errors = "Not Found", Status = 404, Successfully = false };
    
    public static BaseResponse<string, T> NotFound<T>(string message, ResponseType type) =>
        new() { Message = message, Successfully = false, Status = 404, Type = type, Errors = "Not Found" };

    public static BaseResponse<string, T> Forbidden<T>(string message, ResponseType type) =>
        new() { Message = message, Successfully = false, Status = 403, Type = type, Errors = "Forbidden" };
}