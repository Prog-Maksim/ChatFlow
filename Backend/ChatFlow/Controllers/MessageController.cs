using System.ComponentModel.DataAnnotations;
using ChatFlow.Models.DB;
using ChatFlow.Models.Requests;
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
[Route("v{version:apiVersion}/messages")]
public class MessagesController(ILogger<MessagesController> logger, IMessageService service): ControllerBase
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
    public async Task<IActionResult> GetMessages([Required] [FromRoute] string chatId, [FromQuery] int limit = 30, [FromQuery] int offset = 0)
    {
        logger.LogInformation("Начало обработки запроса: (все сообщения чата)");
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-messages", "GET").Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-messages", "GET")
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.GetMessagesChatAsync(token, chatId, limit, offset);

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
        logger.LogInformation("Начало обработки запроса: (последнее сообщение)");
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-last-message", "GET").Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-last-message", "GET")
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.GetLastMessageAsync(token, chatId);

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
        logger.LogInformation("Начало обработки запроса: (изменение сообщения)");
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("update-message", "PUT").Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("update-message", "PUT")
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.UpdateMessageAsync(token, chatId, messageId, message);

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
        logger.LogInformation("Начало обработки запроса: (удаление сообщения)");
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("delete-message", "DELETE").Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("delete-message", "DELETE")
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.DeleteMessageAsync(token, chatId, messageId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return NoContent();
        }
    }
    
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
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage([Required] [FromBody] Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Начало обработки запроса: (отправка сообщения)");
        try
        {
            MetricsRegistry.EndpointRequestCounter
                .WithLabels("send-message", "POST").Inc();
        
            using (MetricsRegistry.EndpointDuration
                       .WithLabels("send-message", "POST")
                       .NewTimer())
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                var token = authHeader.Substring("Bearer ".Length);

                var response = await service.SendMessageAsync(token, message, cancellationToken);

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