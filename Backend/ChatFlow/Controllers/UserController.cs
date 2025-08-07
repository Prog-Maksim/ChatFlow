using ChatFlow.Enums;
using ChatFlow.Models.Response;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatFlow.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("v{version:apiVersion}/users")]
public class UserController(ILogger<UserController> logger, IImageService service, IProfileService profileService): ControllerBase
{
    /// <summary>
    /// Выдает публичные ключи пользователя
    /// </summary>
    /// <param name="personId"></param>
    /// <returns></returns>
    [ApiVersion("1.0")]
    [HttpPost("{personId}/public-keys")]
    public async Task<IActionResult> UploadAvatar(string personId)
    {
        logger.LogInformation("Начало обработки запроса: (выдача публичных ключей)");
        var response = await profileService.GetPublicKeyAsync(personId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
    
    /// <summary>
    /// Добавляет изображение профилю
    /// </summary>
    /// <param name="file">Изображение jpg</param>
    /// <param name="top">Отступ сверху (в пикселях)</param>
    /// <param name="left">Отступ слева (в пикселях)</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="400">Некорректное изображение</response>
    /// <response code="403">Невалидный jwt токен или изображение слишком большое</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="409">Допущено предельное кол-во изображений</response>
    /// <response code="500">Не удалось обработать изображение</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPost("me/avatar")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, [FromQuery] double? top = 0,
        [FromQuery] double? left = 0)
    {
        logger.LogInformation("Начало обработки запроса: (добавление изображения)");
        if (file.Length == 0)
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse<string, string>
            {
                Message = "Файл отсутствует.",
                Type = ResponseType.ImageNotFound,
                Successfully = false, Status = 400,
                Errors = "BadRequest", Data = null
            });

        if (!file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse<string, string>
            {
                Message = "Поддерживается только формат JPG.",
                Type = ResponseType.InvalidFile,
                Successfully = false, Status = 400,
                Errors = "BadRequest", Data = null
            });

        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.UploadFile(file, token, top, left);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Выдает кол-во изображений у пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("me/images/count")]
    [ProducesResponseType(typeof(BaseResponse<string, CountImage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, CountImage>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, CountImage>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CountImages()
    {
        logger.LogInformation("Начало обработки запроса: (кол-во изображений пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetCountImages(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Выдает кол-во изображений у пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{personId}/images/count")]
    [ProducesResponseType(typeof(BaseResponse<string, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, int>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, int>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CountImages([FromRoute] string personId)
    {
        logger.LogInformation("Начало обработки запроса: (кол-во изображений пользователя)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetCountImages(token, personId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Выдает главное изображение пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь или главное изображение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("me/images/primary")]
    [ProducesResponseType(typeof(BaseResponse<string, DataImage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, DataImage>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, DataImage>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrimaryImages()
    {
        logger.LogInformation("Начало обработки запроса: (главное изображение)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetPrimaryImage(token);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Выдает главное изображение пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь или главное изображение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpGet("{personId}/images/primary")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrimaryImages([FromRoute] string personId)
    {
        logger.LogInformation("Начало обработки запроса: (главное изображение)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.GetPrimaryImage(token, personId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Делает изображение основным
    /// </summary>
    /// <param name="imageId">Идентификатор изображения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Изображение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpPut("me/images/{imageId}/primary")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrimaryImages([FromRoute] string imageId)
    {
        logger.LogInformation("Начало обработки запроса: (установка основного изображения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.SetImageIsPrimary(token, imageId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }

    /// <summary>
    /// Удаляет изображение
    /// </summary>
    /// <param name="imageId">Идентификатор изображения</param>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Изображение не найдено</response>
    [Authorize]
    [ApiVersion("1.0")]
    [HttpDelete("me/images/{imageId}")]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImages([FromRoute] string imageId)
    {
        logger.LogInformation("Начало обработки запроса: (удаление изображения)");
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);

        var response = await service.DeleteImage(token, imageId);

        if (!response.Successfully)
            return StatusCode(response.Status, response);

        return Ok(response);
    }
}