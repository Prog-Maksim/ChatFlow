using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/profiles")]
public class ProfileController(ILogger<ProfileController> logger, IProfileService service): ControllerBase
{
    /// <summary>
    /// Возвращает краткую информацию о пользователе
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("me/summary")]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileSummary()
    {
        logger.LogInformation("Начало обработки запроса: (краткая информация пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetSummaryProfileData(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Возвращает краткую информацию о пользователе
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{personId?}/summary")]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileSummary([FromRoute] string personId)
    {
        logger.LogInformation("Начало обработки запроса: (краткая информация пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetSummaryProfileData(token, personId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Возвращает все фотографии пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("me/images")]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataImage>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileImages()
    {
        logger.LogInformation("Начало обработки запроса: (все фотографии пользователя)");

        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetProfileImages(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Возвращает все фотографии пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{personId}/images")]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataImage>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileImages([FromRoute] string personId)
    {
        logger.LogInformation("Начало обработки запроса: (все фотографии пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetProfileImages(token, personId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Возвращает полную информацию о пользователе
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("")]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullProfile()
    {
        logger.LogInformation("Начало обработки запроса: (полная информация пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetProfileData(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Возвращает полную информацию о пользователе
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{personId}")]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullProfile([FromRoute] string personId)
    {
        logger.LogInformation("Начало обработки запроса: (полная информация пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetProfileData(token, personId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Обновляет информацию в профиле
    /// </summary>
    /// <param name="profile">Данные профиля</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="409">Тег занят</response>
    [Authorize]
    [HttpPut]
    [ApiVersion("1.0")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile([FromBody] Profile profile)
    {
        logger.LogInformation("Начало обработки запроса: (обновление информации в профиле)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.UpdateProfileData(token, profile);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
}