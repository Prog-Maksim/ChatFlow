using API.Models.DB;
using API.Models.Responce;
using Microsoft.AspNetCore.Mvc;

namespace API.Security;

public static class UserValidator
{
    public static async Task<IActionResult?> ValidateUser(HttpContext httpContext, List<Person> clients, string? action = null, string? name = null)
    {
        var userIpAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? httpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(userIpAddress))
        {
            return new BadRequestObjectResult(new RegistrationRequests
            {
                Success = false,
                message = "Не удалось определить IP-адрес пользователя",
                ErrorCode = "400",
                Error = "Bad Request"
            });
        }

        bool isFromRussia = await DeterminingIPAddress.IsUserFromRussia(userIpAddress);
        if (!isFromRussia)
        {
            return new ObjectResult(new RegistrationRequests
            {
                Success = false,
                message = "Регистрация возможна только для пользователей из России",
                ErrorCode = "451",
                Error = "Unavailable For Legal Reasons"
            })
            { StatusCode = 451 };
        }

        if (!httpContext.Request.Headers.ContainsKey("User-Agent"))
        {
            return new BadRequestObjectResult(new RegistrationRequests
            {
                Success = false,
                message = "Отсутствует параметр 'User-Agent'",
                ErrorCode = "400",
                Error = "Bad Request"
            });
        }

        if (action == null || name == null)
            return null;

        if (action == "registration" && clients.Any(c => c.Name == name))
        {
            return new ObjectResult(new RegistrationRequests
            {
                Success = false,
                message = "Данный пользователь уже существует",
                ErrorCode = "403",
                Error = "Forbidden"
            })
            { StatusCode = 403 };
        }

        if (action == "authorization" && clients.All(c => c.Name != name))
        {
            return new BadRequestObjectResult(new RegistrationRequests
            {
                Success = false,
                message = "Данный пользователь не найден",
                ErrorCode = "400",
                Error = "Bad Request"
            });
        }

        return null; 
    }
}
