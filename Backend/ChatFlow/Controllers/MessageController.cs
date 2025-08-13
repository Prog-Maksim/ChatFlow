using System.ComponentModel.DataAnnotations;
using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/chats")]
public class MessagesController(ILogger<MessagesController> logger, IMessageService service): ControllerBase
{
    /// <summary>
    /// Выдает все сообщения с пагинацией
    /// </summary>
    /// <remarks>
    /// Если для секретного чата поля Signature и Keys отсутствует, то сообщение было переслано из другого чата и его можно не расшифровывать 
    /// </remarks>
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
    [ProducesResponseType(typeof(BaseResponse<string, MessagesPagination>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, MessagesPagination>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessagesPagination>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages([Required] [FromRoute] string chatId, [FromQuery] int limit = 30,
        [FromQuery] int offset = 0)
    {
        logger.LogInformation("Начало обработки запроса: (все сообщения чата)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetMessagesChatAsync(token, chatId, limit, offset);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Выдает последнее сообщение чата
    /// </summary>
    /// <remarks>
    /// Если для секретного чата поля Signature и Keys отсутствует, то сообщение было переслано из другого чата и его можно не расшифровывать 
    /// </remarks>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или пользователь не состоит в чате</response>
    /// <response code="404">Чат или сообщения не найдены</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{chatId}/messages/last")]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageLast([Required] [FromRoute] string chatId)
    {
        logger.LogInformation("Начало обработки запроса: (последнее сообщение)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetLastMessageAsync(token, chatId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Возвращает информацию о сообщении
    /// </summary>
    /// <remarks>
    /// Нужна например, чтобы узнать содержимое пересылаемого сообщения
    /// </remarks>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{chatId}/messages/{messageId}")]
    public async Task<IActionResult> GetMessage([Required] [FromRoute] string chatId, [Required] [FromRoute] string messageId)
    {
        logger.LogInformation("Начало обработки запроса: (данные сообщения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetMessageDataAsync(token, chatId, messageId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    
    /// <summary>
    /// Позволяет изменить сообщение
    /// </summary>
    /// <remarks>
    /// Пересылаемое сообщение нельзя изменить
    /// </remarks>
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
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMessage([Required] [FromRoute] string chatId,
        [Required] [FromRoute] string messageId, [Required] [FromBody] UpdateMessage message)
    {
        logger.LogInformation("Начало обработки запроса: (изменение сообщения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.UpdateMessageAsync(token, chatId, messageId, message);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Позволяет удалить сообщение
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    /// <response code="204">Успешно</response>
    /// <response code="400">Сообщение не было удалено</response>
    /// <response code="403">Невалидный jwt токен или пользователь не состоит в чате или нет прав на редактирование</response>
    /// <response code="404">Чат или сообщения не найдены</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("{chatId}/messages/{messageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, MessageData>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage([Required] [FromRoute] string chatId,
        [Required] [FromRoute] string messageId)
    {
        logger.LogInformation("Начало обработки запроса: (удаление сообщения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.DeleteMessageAsync(token, chatId, messageId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return NoContent();
    }
    
    /// <summary>
    /// Позволяет отправить сообщение
    /// </summary>
    /// <param name="chatId">Идентификатор чата в который отправить сообщение</param>
    /// <param name="message">Данные сообщения</param>
    /// <param name="cancellationToken">Токен отмены отправки сообщения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или запрещено отправлять сообщения</response>
    /// <response code="404">Чат не найден</response>
    [Authorize]
    [HttpPost("{chatId}/messages/send")]
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage([Required] [FromRoute] string chatId, [FromBody] Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Начало обработки запроса: (отправка сообщения)");
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.SendMessageAsync(token, chatId, message, cancellationToken);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest, "Client closed request");
        }
    }

    /// <summary>
    /// Позволяет отправить сообщение с ответом
    /// </summary>
    /// <param name="chatId">Идентификатор чата в который отправить сообщение</param>
    /// <param name="messageId">Идентификатор отвечаемого сообщения</param>
    /// <param name="message">Данные сообщения</param>
    /// <param name="cancellationToken">Токен отмены отправки сообщения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или запрещено отправлять сообщения</response>
    /// <response code="404">Чат или сообщение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPost("{chatId}/messages/{messageId}/reply")]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendReplyMessage([Required] [FromRoute] string chatId, [Required] [FromRoute] string messageId, [FromBody] Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Начало обработки запроса: (отправка сообщения c ответом)");
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.SendReplyMessageAsync(token, chatId, messageId, message, cancellationToken);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest, "Client closed request");
        }
    }

    /// <summary>
    /// Позволяет переслать сообщение
    /// </summary>
    /// <remarks>
    /// Если передан объект message, то оно будет отправлено ответом на пересылаемое сообщение и в ответе будет объект нового сообщения, а не пересылаемого 
    /// </remarks>
    /// <param name="chatId">Идентификатор чата откуда переслать сообщение</param>
    /// <param name="messageId">Идентификатор пересылаемого сообщения</param>
    /// <param name="request">Данные для пересылки сообщения</param>
    /// <param name="cancellationToken">Токен отмены отправки сообщения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или запрещено отправлять сообщения</response>
    /// <response code="404">Чат или сообщение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPost("{chatId}/messages/{messageId}/forward")]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SendMessage>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage1([Required] [FromRoute] string chatId, [Required] [FromRoute] string messageId, [FromBody] ForwardMessageRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Начало обработки запроса: (отправка пересылаемого сообщения)");
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await service.SendForwardMessageAsync(token, chatId, messageId, request, cancellationToken);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest, "Client closed request");
        }
    }
    
    /// <summary>
    /// Позволяет отметить сообщение как прочитанное
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен, не состоишь в чате, запрещено для этого сообщения</response>
    /// <response code="404">Чат или сообщение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPost("{chatId}/messages/{messageId}/view")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMessageTheView([Required] [FromRoute] string chatId, [Required]  [FromRoute] string messageId)
    {
        logger.LogInformation("Начало обработки запроса: (прочтение сообщения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.ReadTheMessage(token, chatId, messageId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
    
    /// <summary>
    /// Выдача просмотров для сообщения
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен, не состоишь в чате, запрещено для этого сообщения</response>
    /// <response code="404">Чат или сообщение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{chatId}/messages/{messageId}/view")]
    [ProducesResponseType(typeof(BaseResponse<string, List<PersonReadMessage>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, List<PersonReadMessage>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, List<PersonReadMessage>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageTheView([Required] [FromRoute] string chatId, [Required]  [FromRoute] string messageId)
    {
        logger.LogInformation("Начало обработки запроса: (выдача просмотров для сообщения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetTheReadMessage(token, chatId, messageId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Выдача кол-ва просмотров для сообщения
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен, не состоишь в чате</response>
    /// <response code="404">Чат или сообщение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{chatId}/messages/{messageId}/view/count")]
    [ProducesResponseType(typeof(BaseResponse<string, CountReadMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, CountReadMessage>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, CountReadMessage>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCountViewTheMessageAsync([Required] [FromRoute] string chatId,
        [Required] [FromRoute] string messageId)
    {
        logger.LogInformation("Начало обработки запроса: (выдача кол-ва просмотров для сообщения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetTheCountReadMessage(token, chatId, messageId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
}