using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using SearchService.Monitoring;

namespace SearchService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class SearchController(ILogger<SearchController> logger, Service.SearchService searchService): ControllerBase
{
    /// <summary>
    /// Производит поиск чатов и пользователей
    /// </summary>
    /// <param name="query">Поисковой запрос</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("search", "GET", "search", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("search", "GET", "search", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await searchService.SearchAsync(token, query);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
}