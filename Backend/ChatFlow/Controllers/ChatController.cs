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
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
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
}