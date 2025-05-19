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
    public async Task<IActionResult> RegistrationUser([FromBody][Required] RegistrationUser registrationUser)
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
        
            if (userIpAddress == null)
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
    public async Task<IActionResult> AuthorizationUser([FromBody][Required] AuthUser authUser)
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
        
            if (userIpAddress == null)
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

    /// <summary>
    /// Добавление двухфакторной аутентификации через Google Authenticator
    /// </summary>
    /// <param name="code">Код авторизации</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="404">Код авторизации не найден</response>
    [AllowAnonymous]
    [HttpPost("enable-2fa")]
    [ProducesResponseType(typeof(BaseResponse<string, Token2Fa>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, Token2Fa>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, Token2Fa>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGoogleAuthenticator([Required][FromQuery] string code)
    {
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");
        
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("2FA", "POST", "auth", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("2FA", "POST", "auth", Environment.MachineName)
                   .NewTimer())
        {
            string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
            if (userIpAddress == null)
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
            
            var response =  await authService.AddGoogleAuthenticatorAsync(code, userIpAddress);
        
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }

    /// <summary>
    /// Проверяет код полученный из Google Authenticator
    /// </summary>
    /// <param name="code">Код авторизации</param>
    /// <param name="key">Проверочный код</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Код не верен или достигнуто максимальное кол-во устройств</response>
    /// <response code="404">Код авторизации не найден</response>
    [AllowAnonymous]
    [HttpPost("verify-code")]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckCode([Required][FromQuery] string code, [Required][FromQuery] string key)
    {
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");
        
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("check-code", "POST", "auth", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("check-code", "POST", "auth", Environment.MachineName)
                   .NewTimer())
        {
            string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
            if (userIpAddress == null)
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
            
            var response = await authService.CheckGoogleAuthenticatorAsync(code, key, userIpAddress, userAgent);
        
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }

    /// <summary>
    /// Генерирует qr-code для Google Authenticator
    /// </summary>
    /// <param name="code">Код авторизации</param>
    /// <returns></returns>
    /// <response code="200">Успешно (картинка)</response>
    /// <response code="403">Qr-code не может быть создан!</response>
    /// <response code="404">Данные не найдены</response>
    [AllowAnonymous]
    [HttpGet("qr-code")]
    [ProducesResponseType(typeof(byte[]),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode([Required][FromQuery] string code)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("generate-qrcode", "GET", "auth", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("generate-qrcode", "POST", "auth", Environment.MachineName)
                   .NewTimer())
        {
            string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
            if (userIpAddress == null)
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
            
            try
            {
                var response = await authService.GetQrCodeGoogleAuthenticatorAsync(code, userIpAddress);
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
    }

    /// <summary>
    /// Обновление токена
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="423">Пользователь не найден или был заблокирован</response>
    [Authorize]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status423Locked)]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");
        
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("refresh-token", "POST", "auth", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("refresh-token", "POST", "auth", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);
        
            var response = await authService.RefreshAccessToken(token);
        
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }

    /// <summary>
    /// Удаляет сессию или все если не передан id сессии
    /// </summary>
    /// <param name="session">Идентификатор сессии</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Активные сессии не найдены</response>
    [Authorize]
    [HttpDelete("sessions")]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessions([FromQuery] string? session)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("delete-sessions", "DELETE", "auth", Environment.MachineName).Inc();

        using (MetricsRegistry.EndpointDuration
                   .WithLabels("delete-sessions", "DELETE", "auth", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await authService.RevokeSession(token, session);
            
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }

    /// <summary>
    /// Возвращает все активные сессии
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataSession>>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataSession>>),StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessions()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-sessions", "GET", "auth", Environment.MachineName).Inc();

        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get_sessions", "GET", "auth", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);
            
            var response = await authService.GetSessions(token);
            
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }
}