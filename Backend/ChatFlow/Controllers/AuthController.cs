using System.ComponentModel.DataAnnotations;
using ChatFlow.Enums;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/auth")]
public class AuthController(ILogger<AuthController> logger, IAuthService service): ControllerBase
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <remarks>
    /// <b>Требует обязательную передачу User-Agent.</b>
    /// </remarks>
    /// <param name="registrationUser">Данные пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешная регистрация нового пользователя</response>
    /// <response code="406">Не удалось определить Ip адрес пользователя</response>
    /// <response code="400">Ошибка валидации данных</response>
    /// <response code="403">Номер телефона занят</response>
    [AllowAnonymous]
    [ApiVersion("2.0")]
    [HttpPost("registration")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<object, object>),StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegistrationUser([FromBody] [Required] RegistrationUser registrationUser)
    {
        logger.LogInformation("Начало обработки запроса: (регистрация пользователя)");
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");
        
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
        
        var response = await service.RegistrationUserAsync(registrationUser, userIpAddress);
        
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Авторизация пользователя
    /// </summary>
    /// <remarks>
    /// <b>Требует обязательную передачу User-Agent.</b>
    /// </remarks>
    /// <param name="authUser">Данные пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешная авторизация пользователя</response>
    /// <response code="400">Ошибка валидации данных</response>
    /// <response code="403">Неверный пароль, заблокированный аккаунт, итд</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="406">Не удалось определить Ip адрес пользователя</response>
    /// <response code="429">Слишком много попыток входа</response>
    [AllowAnonymous]
    [ApiVersion("1.0")]
    [HttpPost("authorization")]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AuthorizationUser([FromBody] [Required] AuthUser authUser)
    {
        logger.LogInformation("Начало обработки запроса: (авторизация пользователя)");
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");
        
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
        
        var response = await service.AuthorizationUserAsync(authUser.Login, authUser.Password, userIpAddress, authUser.PublicKey, authUser.RefreshToken);
        
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
    }
}