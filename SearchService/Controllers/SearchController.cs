using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchService.Repository.Interfaces;

namespace SearchService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class SearchController(ILogger<SearchController> logger): ControllerBase
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
        
    }
}