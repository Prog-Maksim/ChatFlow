using System.ComponentModel.DataAnnotations;
using MessageService.Models.Requests;
using MessageService.Models.Response;
using MessageService.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace MessageService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class MessagesController(ILogger<MessagesController> logger, Service.MessageService messageService): ControllerBase
{
    /// <summary>
    /// Позволяет отправить сообщение
    /// </summary>
    /// <param name="message">Данные сообщения</param>
    /// <param name="cancellationToken">Токен отмены сообщения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или запрещено отправлять сообщения</response>
    /// <response code="404">Чат не найден</response>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage([Required] [FromBody] Message message, CancellationToken cancellationToken)
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
}