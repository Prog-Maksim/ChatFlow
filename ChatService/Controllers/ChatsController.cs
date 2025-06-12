using System.ComponentModel.DataAnnotations;
using ChatService.Models.Requests;
using ChatService.Models.Response;
using ChatService.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace ChatService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class ChatsController(ILogger<ChatsController> logger, Service.ChatService chatService): ControllerBase
{
    /// <summary>
    /// Создает личный чат между двумя пользователями
    /// </summary>
    /// <param name="request">Идентификатор второго пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Невозможно создать чат</response>
    [Authorize]
    [HttpPost("private")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePrivateChat([Required][FromBody] CreatePrivateChatRequest request)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("create-private-chat", "POST", "chat", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("create-private-chat", "POST", "chat", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await chatService.CreatePrivateChat(token, request.ParticipantId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }

    /// <summary>
    /// Возвращает все чаты пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<string, Chats>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, Chats>),StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChats()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-chats", "GET", "chat", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-chats", "GET", "chat", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await chatService.GetChats(token);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
}