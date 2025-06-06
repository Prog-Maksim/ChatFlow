using System.ComponentModel.DataAnnotations;
using ChatService.Monitoring;
using ChatService.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace ChatService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class ChatController(ILogger<ChatController> logger, Service.ChatService chatService): ControllerBase
{
    /// <summary>
    /// Создает личный чат между двумя пользователями
    /// </summary>
    /// <param name="otherPersonId">Идентификатор второго пользователя</param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("private")]
    public async Task<IActionResult> CreatePrivateChat([Required][FromQuery] string otherPersonId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("search", "GET", "search", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("search", "GET", "search", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await chatService.CreatePrivateChat(token, otherPersonId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
}