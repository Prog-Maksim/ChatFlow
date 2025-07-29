using System.ComponentModel.DataAnnotations;
using ChatFlow.Enums;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/account")]
public class AccountController(ILogger<AuthController> logger, AccountService service): ControllerBase
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
    [ApiVersion("1.0")]
    [HttpPatch("phone")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhoneNumber([Required] [FromBody] UpdatePhoneRequest request)
    {
        logger.LogInformation("Начало обработки запроса: (обновление номера телефона)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
            
        var response = await service.UpdateNumberPhone(token, request.PhoneNumber);
            
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
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
    [ApiVersion("1.0")]
    [HttpPatch("password")]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, RegistrationCode>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePassword([Required] [FromBody] UpdatePassword requests)
    {
        logger.LogInformation("Начало обработки запроса: (обновление пароля)");
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

        var response = await service.UpdatePassword(token, requests.OldPassword, requests.NewPassword, userIpAddress);
            
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Позволяет выйти из аккаунта
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен и невалидный пароль</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPost("exit")]
    [ProducesResponseType(typeof(BaseResponse<string, RevokeSession>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Exit()
    {
        logger.LogInformation("Начало обработки запроса: (выход из аккаунта)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
            
        var response = await service.ExitTheSession(token);
            
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
    }
}