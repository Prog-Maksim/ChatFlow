using AuthService.Enums;
using AuthService.Models.Response;

namespace AuthService.Scripts;

public static class ResponseFactory
{
    public static BaseResponse<string, TData> JwtTokenInvalid<TData>() =>
        new() { Message = "JWT токен недействителен", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false };

    public static BaseResponse<string, TData> PersonNotFound<TData>() =>
        new() { Message = "Пользователь не найден", Type = ResponseType.PersonNotFound, Errors = "Not Found", Status = 404, Successfully = false };

    public static BaseResponse<string, TData> InvalidPassword<TData>(string message) =>
        new() { Message = message, Type = ResponseType.InvalidPasswordError, Errors = "Forbidden", Status = 403, Successfully = false };
    
    public static BaseResponse<string, TData> PhoneNumberInUse<TData>() =>
        new() { Message = "Данный номер телефона занят", Successfully = false, Status = 403, Type = ResponseType.PhoneNumberInUse, Errors = "Forbidden" };
    
    public static BaseResponse<string, TData> TooManyRequests<TData>() =>
        new() { Status = 429, Message = "Слишком много попыток входа. Попробуйте еще раз позже.", Type = ResponseType.TooManyRequests, Errors = "Too Many Requests", Successfully = false };

    public static BaseResponse<string, TData> Success<TData>(string message, TData data) =>
        new() { Message = message, Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = data };
}