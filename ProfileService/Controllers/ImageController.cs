using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Monitoring;
using ProfileService.Service;
using Prometheus;

namespace ProfileService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class ImageController(ILogger<ImageController> _logger, ImageService imageService): ControllerBase
{
    /// <summary>
    /// Добавляет изображение профилю (до 25 штук)
    /// </summary>
    /// <param name="file">Изображение jpg</param>
    /// <param name="top">Отступ сверху (в пикселях)</param>
    /// <param name="left">Отступ слева (в пикселях)</param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("images")]
    public async Task<IActionResult> UploadImages(IFormFile file, [FromQuery] double? top = 0, [FromQuery] double? left = 0)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("upload-images", "POST", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("upload-images", "POST", "profile", Environment.MachineName)
                   .NewTimer())
        {
            if (file.Length == 0)
                return BadRequest("Файл отсутствует.");

            if (!file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Поддерживается только формат JPG.");

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
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <remarks>
    /// Если параметр <c>personId</c> не указан, метод вернёт информацию о текущем пользователе,
    /// основываясь на идентификаторе из JWT токена.
    /// </remarks>
    [Authorize]
    [HttpGet("count-images")]
    public async Task<IActionResult> CountImages([FromQuery] string? personId = null)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("count-images", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("count-images", "GET", "profile", Environment.MachineName)
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
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <remarks>
    /// Если параметр <c>personId</c> не указан, метод вернёт информацию о текущем пользователе,
    /// основываясь на идентификаторе из JWT токена.
    /// </remarks>
    [Authorize]
    [HttpGet("primary-images")]
    public async Task<IActionResult> GetPrimaryImages([FromQuery] string? personId = null)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-primary-images", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-primary-images", "GET", "profile", Environment.MachineName)
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
    [Authorize]
    [HttpPut("primary-images")]
    public async Task<IActionResult> UpdatePrimaryImages([Required] [FromQuery] string imageId)
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
    [Authorize]
    [HttpDelete("images")]
    public async Task<IActionResult> DeleteImages([Required] [FromQuery] string imageId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("delete-images", "DELETE", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("delete-images", "DELETE", "profile", Environment.MachineName)
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