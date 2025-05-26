using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Models.Requests;
using ProfileService.Monitoring;
using Prometheus;

namespace ProfileService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class ProfileController(ILogger<ProfileController> _logger, Service.ProfileService profileService): ControllerBase
{
    /// <summary>
    /// Возвращает краткую информацию о пользователе
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <remarks>
    /// Если параметр <c>personId</c> не указан, метод вернёт информацию о текущем пользователе,
    /// основываясь на идентификаторе из JWT токена.
    /// </remarks>
    [Authorize]
    [HttpGet("profiles/{personId?}/summary")]
    public async Task<IActionResult> GetProfileSummary([FromRoute] string? personId = null)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("summary-profile", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("summary-profile", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await profileService.GetSummaryProfileData(token, personId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
    
    /// <summary>
    /// Возвращает полную информацию о пользователе
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    /// <remarks>
    /// Если параметр <c>personId</c> не указан, метод вернёт информацию о текущем пользователе,
    /// основываясь на идентификаторе из JWT токена.
    /// </remarks>
    [Authorize]
    [HttpGet("profiles/{personId?}")]
    public async Task<IActionResult> GetFullProfile([FromRoute] string? personId = null)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("profile", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("profile", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await profileService.GetProfileData(token, personId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }

    /// <summary>
    /// Обновляет информацию в профиле
    /// </summary>
    /// <param name="profile">Данные профиля</param>
    /// <returns></returns>
    [Authorize]
    [HttpPut("profiles")]
    public async Task<IActionResult> UpdateProfile([FromBody] Profile profile)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("update-profile", "PUT", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("update-profile", "PUT", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await profileService.UpdateProfileData(token, profile);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
}