using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfileService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class ProfileController(ILogger<ProfileController> _logger, Service.ProfileService _profileService): ControllerBase
{
    // TODO: Дополнить документацию ручек
    // TODO: Реализовать мониторинг сервисов
    
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
        if (file.Length == 0)
            return BadRequest("Файл отсутствует.");

        if (!file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Поддерживается только формат JPG.");

        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await _profileService.UploadFile(file, token, top, left);
        
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Выдает кол-во изображений у пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("count-images")]
    public async Task<IActionResult> CountImages([FromQuery] string? personId = null)
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await _profileService.GetCountImages(token, personId);
        
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
    }

    /// <summary>
    /// Выдает главное изображение пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("primary-images")]
    public async Task<IActionResult> GetPrimaryImages([FromQuery] string? personId = null)
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await _profileService.GetPrimaryImage(token, personId);
        
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
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
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await _profileService.SetImageIsPrimary(token, imageId);
        
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
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
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        var response = await _profileService.DeleteImage(token, imageId);
        
        if (!response.Successfully)
            return StatusCode(response.Status, response);
        
        return Ok(response);
    }
}