using System.ComponentModel.DataAnnotations;
using AuthService.Models.Requests;
using AuthService.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <param name="registrationUser"></param>
    /// <returns></returns>
    /// <response code="200">Успешная регистрация нового пользователя</response>
    /// <response code="406">Не удалось определить Ip адрес пользователя</response>
    /// <response code="400">Ошибка валидации данных</response>
    /// <response code="403">Номер телефона занят</response>
    [AllowAnonymous]
    [HttpPost("registration")]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegistrationUser([FromBody][Required] RegistrationUser registrationUser)
    {
        string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
        if (userIpAddress == null)
        {
            var error = new BaseResponse
            {
                Message = "Невозможно определить ip адрес",
                Success = false,
                StatusCode = 406,
                Error = "Not Acceptable"
            };
            logger.LogError("Невозможно определить ip адрес");
            return StatusCode(error.StatusCode, error);
        }
        
        var response = await authService.RegistrationUserAsync(registrationUser, userIpAddress);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Авторизация пользователя
    /// </summary>
    /// <param name="authUser">Данные пользователя</param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("authorization")]
    public async Task<IActionResult> AuthorizationUser([FromBody][Required] AuthUser authUser)
    {
        string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
        if (userIpAddress == null)
        {
            var error = new BaseResponse
            {
                Message = "Невозможно определить ip адрес",
                Success = false,
                StatusCode = 406,
                Error = "Not Acceptable"
            };
            logger.LogError("Невозможно определить ip адрес");
            return StatusCode(error.StatusCode, error);
        }
        
        var response = await authService.AuthorizationUserAsync(authUser.Login, authUser.Password, userIpAddress);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Добавление двухфакторной аутентификации через Google Authenticator
    /// </summary>
    /// <param name="code">Код авторизации</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="404">Код авторизации не найден</response>
    [AllowAnonymous]
    [HttpPost("add-google-authenticator")]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGoogleAuthenticator([Required][FromQuery] string code)
    {
        var response =  await authService.AddGoogleAuthenticatorAsync(code);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Проверяет код полученный из Google Authenticator
    /// </summary>
    /// <param name="code">Код авторизации</param>
    /// <param name="key">Проверочный код</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="404">Код авторизации не найден</response>
    [AllowAnonymous]
    [HttpGet("check-code")]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckCode([Required][FromQuery] string code, [Required][FromQuery] string key)
    {
        var response = await authService.CheckGoogleAuthenticatorAsync(code, key);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Генерирует qr-code для Google Authenticator
    /// </summary>
    /// <param name="code">Код авторизации</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="404">Данные не найдены</response>
    [AllowAnonymous]
    [HttpGet("qr-code")]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode([Required][FromQuery] string code)
    {
        try
        {
            var response = await authService.GetQrCodeGoogleAuthenticatorAsync(code);
            return File(response, "image/png");
        }
        catch (NullReferenceException error)
        {
            logger.LogError(error.Message);
            return StatusCode(StatusCodes.Status404NotFound, error.Message);
        }
        catch (UnauthorizedAccessException error)
        {
            logger.LogError(error.Message);
            return StatusCode(StatusCodes.Status403Forbidden, error.Message);
        }
    }

    /// <summary>
    /// Обновление токена
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await authService.RefreshAccessToken(token);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }
}