using System.ComponentModel.DataAnnotations;
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
public class ChatsController(ILogger<ChatsController> logger, IChatService service): ControllerBase
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
    [ApiVersion("1.0")]
    [HttpPost("private")]
    [ProducesResponseType(typeof(BaseResponse<string, CreateChat>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePrivateChat([Required] [FromBody] CreatePrivateChatRequest request)
    {
        logger.LogInformation("Начало обработки запроса: (создание личного чата)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.CreatePrivateChat(token, request.ParticipantId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
    
    /// <summary>
    /// Создает секретный чат между двумя пользователями
    /// </summary>
    /// <remarks>
    /// Секретный чат создается поверх личного, заменяя его
    /// </remarks>
    /// <param name="request">Идентификатор второго пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен или не создан обычный чат</response>
    /// <response code="404">Невозможно создать чат</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPost("secret")]
    [ProducesResponseType(typeof(BaseResponse<string, CreateChat>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSecretPrivateChat([Required] [FromBody] CreatePrivateChatRequest request)
    {
        logger.LogInformation("Начало обработки запроса: (создание защищенного личного чата)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.CreateSecretPrivateChat(token, request.ParticipantId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Возвращает все чаты пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    [Authorize]
    [HttpGet]
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, Chats>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, Chats>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChats()
    {
        logger.LogInformation("Начало обработки запроса: (все чаты пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetChats(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Выдает информацию о чате
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Пользователь не состоит в чате или токен не валиден</response>
    /// <response code="404">Чат не найден</response>
    [Authorize]
    [HttpGet("{chatId}/info")]
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, ChatInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChatInfo([Required] [FromRoute] string chatId)
    {
        logger.LogInformation("Начало обработки запроса: (информация чата)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetChatInfo(token, chatId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
    
    /// <summary>
    /// Очищает историю чата
    /// </summary>
    /// <remarks>
    /// Если чат секретный, то чат удалится для обоих собеседников
    /// </remarks>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="400">Не удалось очистить историю чата</response>
    /// <response code="403">Невалидный jwt токен, пользователь не состоит в чате или токен не валиден</response>
    /// <response code="404">Чат не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("{chatId}/messages")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAllMessages(string chatId)
    {
        logger.LogInformation("Начало обработки запроса: (удаление истории чата)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await service.DeleteAllMessages(token, chatId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
    
    /// <summary>
    /// Очищает историю чата для обоих собеседников
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="400">Не удалось очистить историю чата</response>
    /// <response code="403">Невалидный jwt токен, пользователь не состоит в чате или токен не валиден</response>
    /// <response code="404">Чат не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("{chatId}/messages/all")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAllMessagesAllPersons(string chatId)
    {
        logger.LogInformation("Начало обработки запроса: (удаление истории чата для всех)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await service.DeleteAllMessages(token, chatId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Удаляет чат только для себя
    /// </summary>
    /// <remarks>
    /// Если чат секретный, то чат удалится для обоих собеседников
    /// </remarks>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен, пользователь не состоит в чате или токен не валиден</response>
    /// <response code="404">Чат не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("{chatId}")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChat(string chatId)
    {
        logger.LogInformation("Начало обработки запроса: (удаление чата)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await service.DeleteChat(token, chatId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
    
    /// <summary>
    /// Удаляет чат для обоих собеседников
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен, пользователь не состоит в чате или токен не валиден</response>
    /// <response code="404">Чат не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("{chatId}/all")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChatAllPersons(string chatId)
    {
        logger.LogInformation("Начало обработки запроса: (удаление чата для всех)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await service.DeleteChat(token, chatId, true);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
}