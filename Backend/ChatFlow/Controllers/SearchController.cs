using ChatFlow.Models.Response;
using ChatFlow.Monitoring;
using ChatFlow.Service;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/search")]
public class SearchController(ILogger<SearchController> logger, ISearchService service): ControllerBase
{
    /// <summary>
    /// Производит поиск чатов и пользователей
    /// </summary>
    /// <param name="query">Поисковой запрос</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="503">Поиск ничего не дал</response>
    [Authorize]
    [HttpGet]
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, SearchResult>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SearchResult>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SearchResult>),StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        logger.LogInformation("Начало обработки запроса: (поиск)");
        
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("search", "GET").Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("search", "GET")
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.SearchAsync(token, query);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
}