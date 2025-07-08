using System.ComponentModel.DataAnnotations;
using MessageService.Models.DB;
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
[Route("c/v{version:apiVersion}/chats")]
public class ChatsController(ILogger<ChatsController> logger, Service.MessageService messageService): ControllerBase
{
    /// <summary>
    /// Выдает все сообщения с пагинацией
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="limit">Кол-во сообщений в выдаче</param>
    /// <param name="offset">Отступ от начала списка</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или пользователь не состоит в чате</response>
    /// <response code="404">Чат или сообщения не найдены</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{chatId}/messages")]
    [ProducesResponseType(typeof(BaseResponse<string, MessagesPagination>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, MessagesPagination>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessagesPagination>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages([Required] [FromRoute] string chatId, [FromQuery] int? limit = 30, [FromQuery] int? offset = 0)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-messages", "GET", "message", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-messages", "GET", "message", Environment.MachineName)
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
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или пользователь не состоит в чате</response>
    /// <response code="404">Чат или сообщения не найдены</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{chatId}/messages/last")]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageLast([Required][FromRoute] string chatId)
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
    /// <response code="200">Успешно</response>
    /// <response code="400">Сообщение не было изменено</response>
    /// <response code="403">Невалидный jwt токен или пользователь не состоит в чате или нет прав на редактирование</response>
    /// <response code="404">Чат или сообщения не найдены</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPut("{chatId}/messages/{messageId}")]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMessage([Required][FromRoute] string chatId, [Required][FromRoute] string messageId, [Required][FromBody] UpdateMessage message)
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
    /// <response code="200">Успешно</response>
    /// <response code="400">Сообщение не было удалено</response>
    /// <response code="403">Невалидный jwt токен или пользователь не состоит в чате или нет прав на редактирование</response>
    /// <response code="404">Чат или сообщения не найдены</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("{chatId}/messages/{messageId}")]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage([Required][FromRoute] string chatId, [Required][FromRoute] string messageId)
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

            return NoContent();
        }
    }
}