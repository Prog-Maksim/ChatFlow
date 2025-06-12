using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Enums;
using ProfileService.Models.Response;
using ProfileService.Monitoring;
using ProfileService.Service;
using Prometheus;

namespace ProfileService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class UsersController(ILogger<UsersController> _logger, ImageService imageService): ControllerBase
{
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
    [HttpPost("me/avatar")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, [FromQuery] double? top = 0, [FromQuery] double? left = 0)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("upload-image", "POST", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("upload-image", "POST", "profile", Environment.MachineName)
                   .NewTimer())
        {
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

            var response = await imageService.UploadFile(file, token, top, left);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
    
    /// <summary>
    /// Выдает кол-во изображений у пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpGet("me/images/count")]
    [ProducesResponseType(typeof(BaseResponse<string, CountImage>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, CountImage>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, CountImage>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CountImages()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("count-images", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("count-images", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await imageService.GetCountImages(token);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
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
    [HttpGet("{personId}/images/count")]
    [ProducesResponseType(typeof(BaseResponse<string, int>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, int>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, int>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CountImages([FromRoute] string personId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("count-images-by-id", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("count-images-by-id", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await imageService.GetCountImages(token, personId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
    
    /// <summary>
    /// Выдает главное изображение пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь или главное изображение не найдено</response>
    [Authorize]
    [HttpGet("me/images/primary")]
    [ProducesResponseType(typeof(BaseResponse<string, DataImage>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, DataImage>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, DataImage>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrimaryImages()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-primary-images", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-primary-images", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await imageService.GetPrimaryImage(token);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
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
    [HttpGet("{personId}/images/primary")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrimaryImages([FromRoute] string personId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-primary-images-by-id", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-primary-images-by-id", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await imageService.GetPrimaryImage(token, personId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
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
    [HttpPut("me/images/{imageId}/primary")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrimaryImages([FromRoute] string imageId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("primary-images", "PUT", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("primary-images", "PUT", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await imageService.SetImageIsPrimary(token, imageId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
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
    [HttpDelete("me/images/{imageId}")]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImages([FromRoute] string imageId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("delete-image", "DELETE", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("delete-image", "DELETE", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await imageService.DeleteImage(token, imageId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
}