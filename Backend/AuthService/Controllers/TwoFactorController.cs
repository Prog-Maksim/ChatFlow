using System.ComponentModel.DataAnnotations;
using AuthService.Enums;
using AuthService.Models.Response;
using AuthService.Monitoring;
using AuthService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace AuthService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("a/v{version:apiVersion}/twofactor")]
public class TwoFactorController(ILogger<AuthController> logger, TwoFactorService twoFactorService): ControllerBase
{
    /// <summary>
    /// Включение двухфакторной аутентификации
    /// </summary>
    /// <remarks>
    /// <b>Требует обязательную передачу User-Agent.</b>
    /// </remarks>
    /// <param name="code">Код авторизации</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="404">Код авторизации не найден</response>
    [AllowAnonymous]
    [ApiVersion("1.0")]
    [HttpPost("enable")]
    [ProducesResponseType(typeof(BaseResponse<string, Token2Fa>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, Token2Fa>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, Token2Fa>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGoogleAuthenticator([Required] [FromQuery] string code)
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
            
            var response =  await twoFactorService.AddGoogleAuthenticatorAsync(code, userIpAddress);
        
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }

    /// <summary>
    /// Проверяет код полученный из Google Authenticator
    /// </summary>
    /// <remarks>
    /// <b>Требует обязательную передачу User-Agent.</b>
    /// </remarks>
    /// <param name="code">Код авторизации</param>
    /// <param name="key">Проверочный код</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Код не верен или достигнуто максимальное кол-во устройств</response>
    /// <response code="404">Код авторизации не найден</response>
    [AllowAnonymous]
    [ApiVersion("1.0")]
    [HttpPost("verify")]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckCode([Required] [FromQuery] string code, [Required] [FromQuery] string key)
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
                return StatusCode(error.Status, error);
            }
            
            var response = await twoFactorService.CheckGoogleAuthenticatorAsync(code, key, userIpAddress, userAgent!);
        
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
    [ApiVersion("1.0")]
    [HttpGet("qr-code")]
    [ProducesResponseType(typeof(byte[]),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode([Required] [FromQuery] string code)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("generate-qrcode", "GET", "auth", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("generate-qrcode", "POST", "auth", Environment.MachineName)
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
            
            try
            {
                var response = await twoFactorService.GetQrCodeGoogleAuthenticatorAsync(code, userIpAddress);
                return File(response, "image/png");
            }
            catch (NullReferenceException error)
            {
                logger.LogError(error.Message);
                return StatusCode(StatusCodes.Status404NotFound, new BaseResponse<string, string>
                {
                    Message = error.Message,
                    Type = ResponseType.CodeNotFount,
                    Successfully = false, Status = StatusCodes.Status404NotFound,
                    Data = null, Errors = "Not Fount"
                });
            }
            catch (UnauthorizedAccessException error)
            {
                logger.LogError(error.Message);
                return StatusCode(StatusCodes.Status403Forbidden, new BaseResponse<string, string>
                {
                    Message = error.Message,
                    Type = ResponseType.AccessDenied,
                    Successfully = false, Status = StatusCodes.Status403Forbidden,
                    Data = null, Errors = "Forbidden"
                });
            }
        }
    }
}