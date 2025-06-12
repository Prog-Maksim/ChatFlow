using System.ComponentModel.DataAnnotations;
using AuthService.Enums;
using AuthService.Models.Requests;
using AuthService.Models.Response;
using AuthService.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace AuthService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class AuthController(ILogger<AuthController> logger, Service.AuthService authService): ControllerBase
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="registrationUser">Данные пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешная регистрация нового пользователя</response>
    /// <response code="406">Не удалось определить Ip адрес пользователя</response>
    /// <response code="400">Ошибка валидации данных</response>
    /// <response code="403">Номер телефона занят</response>
    [AllowAnonymous]
    [HttpPost("registration")]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<object, object>),StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegistrationUser([FromBody] [Required] RegistrationUser registrationUser)
    {
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");
        
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("registration", "POST", "auth", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("registration", "POST", "auth", Environment.MachineName)
                   .NewTimer())
        {
            string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
            if (userIpAddress is null)
            {
                var error = new BaseResponse<string, object>
                {
                    Message = "Невозможно определить ip адрес",
                    Successfully = false,
                    Status = 406,
                    Type = ResponseType.IpAddressResolutionFailed,
                    Errors = "Not Acceptable",
                    Data = null
                };
                logger.LogError("Невозможно определить ip адрес");
                return StatusCode(error.Status, error);
            }
        
            var response = await authService.RegistrationUserAsync(registrationUser, userIpAddress);
        
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }

    /// <summary>
    /// Авторизация пользователя
    /// </summary>
    /// <param name="authUser">Данные пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешная авторизация пользователя</response>
    /// <response code="400">Ошибка валидации данных</response>
    /// <response code="403">Неверный пароль, заблокированный аккаунт, итд</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="406">Не удалось определить Ip адрес пользователя</response>
    /// <response code="429">Слишком много попыток входа</response>
    [AllowAnonymous]
    [HttpPost("authorization")]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AuthorizationUser([FromBody] [Required] AuthUser authUser)
    {
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");
        
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("authorization", "POST", "auth", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("authorization", "POST", "auth", Environment.MachineName)
                   .NewTimer())
        {
            string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
            if (userIpAddress is null)
            {
                var error = new BaseResponse<string, object>
                {
                    Message = "Невозможно определить ip адрес",
                    Successfully = false,
                    Status = 406,
                    Type = ResponseType.IpAddressResolutionFailed,
                    Errors = "Not Acceptable",
                    Data = null
                };
                logger.LogError("Невозможно определить ip адрес");
                return StatusCode(error.Status, error);
            }
        
            var response = await authService.AuthorizationUserAsync(authUser.Login, authUser.Password, userIpAddress);
        
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }
}