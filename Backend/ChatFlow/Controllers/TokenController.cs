using ChatFlow.Models.Response;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/token")]
public class TokenController(ILogger<TokenController> logger, ITokenService service): ControllerBase
{
    /// <summary>
    /// Обновление токена
    /// </summary>
    /// <remarks>
    /// <para>Для обновления токенов, требуется передать refresh токен.</para>
    /// <para><b>Требует обязательную передачу User-Agent.</b></para>
    /// </remarks>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="423">Пользователь не найден или был заблокирован</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, AuthTokens>), StatusCodes.Status423Locked)]
    public async Task<IActionResult> RefreshToken()
    {
        logger.LogInformation("Начало обработки запроса: (обновление токена)");
        if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || string.IsNullOrWhiteSpace(userAgent))
            return BadRequest("User-Agent header is missing.");

        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.RefreshAccessToken(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
}