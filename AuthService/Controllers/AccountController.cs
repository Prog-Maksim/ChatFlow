using System.ComponentModel.DataAnnotations;
using AuthService.Enums;
using AuthService.Models.Requests;
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
[Route("backend-a/v{version:apiVersion}/[controller]")]
public class AccountController(ILogger<AuthController> logger, AccountService accountService): ControllerBase
{
    /// <summary>
    /// Позволяет обновить номер телефона
    /// </summary>
    /// <param name="request">Модель с новым номером</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или номер телефона занят</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpPatch("phone")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhoneNumber([Required] [FromBody] UpdatePhoneRequest request)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("update-phone", "PATCH", "auth", Environment.MachineName).Inc();

        using (MetricsRegistry.EndpointDuration
                   .WithLabels("update-phone", "PATCH", "auth", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);
            
            var response = await accountService.UpdateNumberPhone(token, request.PhoneNumber);
            
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }

    /// <summary>
    /// Позволяет обновить пароль
    /// </summary>
    /// <param name="requests">Модель с текущим и новым паролем</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен и невалидный пароль</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpPatch("password")]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePassword([Required] [FromBody] UpdatePassword requests)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("update-password", "PATCH", "auth", Environment.MachineName).Inc();

        using (MetricsRegistry.EndpointDuration
                   .WithLabels("update-password", "PATCH", "auth", Environment.MachineName)
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

            
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await accountService.UpdatePassword(token, requests.OldPassword, requests.NewPassword, userIpAddress);
            
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }
}