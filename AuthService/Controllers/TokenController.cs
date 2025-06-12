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
[Route("backend/v{version:apiVersion}/[controller]")]
public class TokenController(ILogger<AuthController> logger, TokenService tokenService): ControllerBase
{
    /// <summary>
    /// Обновление токена
    /// </summary>
    /// <remarks>
    /// Для обновления токенов, требуется передать refresh токен
    /// </remarks>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="423">Пользователь не найден или был заблокирован</response>
    [Authorize]
    [HttpPost("refresh")]
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
        
            var response = await tokenService.RefreshAccessToken(token);
        
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }
}