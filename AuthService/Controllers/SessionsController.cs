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
public class SessionsController(ILogger<AuthController> logger, SessionService sessionService): ControllerBase
{
    /// <summary>
    /// Возвращает все активные сессии
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    [Authorize]
    [HttpGet]
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
            
            var response = await sessionService.GetSessions(token);
            
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }
    
    /// <summary>
    /// Удаляет все сессии
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Активные сессии не найдены</response>
    [Authorize]
    [HttpDelete]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessions()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("delete-all-sessions", "DELETE", "auth", Environment.MachineName).Inc();

        using (MetricsRegistry.EndpointDuration
                   .WithLabels("delete-all-sessions", "DELETE", "auth", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await sessionService.RevokeSession(token);
            
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
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
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, List<string>>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessions([FromRoute] string sessionId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("delete-session-by-id", "DELETE", "auth", Environment.MachineName).Inc();

        using (MetricsRegistry.EndpointDuration
                   .WithLabels("delete-session-by-id", "DELETE", "auth", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await sessionService.RevokeSession(token, sessionId);
            
            if (!response.Successfully)
                return StatusCode(response.Status, response);
        
            return Ok(response);
        }
    }
}