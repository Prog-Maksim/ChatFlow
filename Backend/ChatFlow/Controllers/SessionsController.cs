using ChatFlow.Models.Response;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/sessions")]
public class SessionsController(ILogger<SessionsController> logger, ISessionService service): ControllerBase
{
    /// <summary>
    /// Возвращает все активные сессии
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    [Authorize]
    [HttpGet]
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataSession>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataSession>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessions()
    {
        logger.LogInformation("Начало обработки запроса: (все сессии)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetSessions(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Удаляет все сессии, кроме текущей
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Активные сессии не найдены</response>
    [Authorize]
    [HttpDelete]
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, List<RevokeSession>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessions()
    {
        logger.LogInformation("Начало обработки запроса: (удаление всех сессий)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.RevokeSession(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Удаляет сессию
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Активные сессии не найдены</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(typeof(BaseResponse<string, List<RevokeSession>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessions([FromRoute] string sessionId)
    {
        logger.LogInformation("Начало обработки запроса: (удаление сессии)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.RevokeSession(token, sessionId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
}