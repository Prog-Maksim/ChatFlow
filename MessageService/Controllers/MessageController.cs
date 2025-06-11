using System.ComponentModel.DataAnnotations;
using MessageService.Models.DB;
using MessageService.Models.Requests;
using MessageService.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace MessageService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class MessageController(ILogger<MessageController> logger, Service.MessageService messageService): ControllerBase
{
    /// <summary>
    /// Позволяет отправить сообщение
    /// </summary>
    /// <param name="message">Данные сообщения</param>
    /// <param name="cancellationToken">Токен отмены сообщения</param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage(Message message, CancellationToken cancellationToken)
    {
        try
        {
            MetricsRegistry.EndpointRequestCounter
                .WithLabels("send-message", "POST", "message", Environment.MachineName).Inc();
        
            using (MetricsRegistry.EndpointDuration
                       .WithLabels("send-message", "POST", "message", Environment.MachineName)
                       .NewTimer())
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                var token = authHeader.Substring("Bearer ".Length);

                var response = await messageService.SendMessageAsync(token, message, cancellationToken);

                if (!response.Successfully)
                    return StatusCode(response.Status, response);

                return Ok(response);
            }
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest, "Client closed request");
        }
    }

    /// <summary>
    /// Выдает все сообщения с пагинацией
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="limit">Кол-во сообщений в выдаче</param>
    /// <param name="offset">Отступ от начала списка</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("message/{chatId}")]
    public async Task<IActionResult> GetMessage([Required][FromRoute] string chatId, [FromQuery] int? limit = 30, [FromQuery] int? offset = 0)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-message", "GET", "message", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-message", "GET", "message", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await messageService.GetMessagesChatAsync(token, chatId, limit.Value, offset.Value);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
    
    /// <summary>
    /// Выдает последнее сообщение чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("message/{chatId}/preview")]
    public async Task<IActionResult> GetMessagePreview([Required][FromRoute] string chatId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-last-message", "GET", "message", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-last-message", "GET", "message", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await messageService.GetLastMessageAsync(token, chatId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }

    /// <summary>
    /// Позволяет изменить сообщение
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщение</param>
    /// <param name="message">Объект обновляемого сообщения</param>
    /// <returns></returns>
    [Authorize]
    [HttpPut("message/{chatId}")]
    public async Task<IActionResult> UpdateMessage([Required][FromBody] string chatId, [Required][FromQuery] string messageId, [Required][FromBody] MessageData message)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("update-message", "PUT", "message", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("update-message", "PUT", "message", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await messageService.UpdateMessageAsync(token, chatId, messageId, message);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }

    /// <summary>
    /// Позволяет удалить сообщение
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    [Authorize]
    [HttpDelete("message/{chatId}")]
    public async Task<IActionResult> DeleteMessage([Required][FromRoute] string chatId, [Required][FromQuery] string messageId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("delete-message", "DELETE", "message", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("delete-message", "DELETE", "message", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await messageService.DeleteMessageAsync(token, chatId, messageId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
}