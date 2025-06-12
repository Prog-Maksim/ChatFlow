using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Models.Other;
using ProfileService.Models.Requests;
using ProfileService.Models.Response;
using ProfileService.Monitoring;
using Prometheus;

namespace ProfileService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class ProfilesController(ILogger<ProfilesController> _logger, Service.ProfileService profileService): ControllerBase
{
    /// <summary>
    /// Возвращает краткую информацию о пользователе
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpGet("me/summary")]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileSummary()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("summary-profile", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("summary-profile", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await profileService.GetSummaryProfileData(token);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
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
    [HttpGet("{personId?}/summary")]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, SummaryDataPerson>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileSummary([FromRoute] string personId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("summary-profile-by-id", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("summary-profile-by-id", "GET", "profile", Environment.MachineName)
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
    /// Возвращает все фотографии пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpGet("me/images")]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataImage>>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileImages()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-images", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-images", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await profileService.GetProfileImages(token);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }
    
    /// <summary>
    /// Возвращает все фотографии пользователя
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpGet("{personId}/images")]
    [ProducesResponseType(typeof(BaseResponse<string, List<DataImage>>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, object>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileImages([FromRoute] string personId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("get-images-by-id", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("get-images-by-id", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await profileService.GetProfileImages(token, personId);

            if (!response.Successfully)
                return StatusCode(response.Status, response);

            return Ok(response);
        }
    }

    /// <summary>
    /// Возвращает полную информацию о пользователе
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpGet("")]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullProfile()
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("profile", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("profile", "GET", "profile", Environment.MachineName)
                   .NewTimer())
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);

            var response = await profileService.GetProfileData(token);

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
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="404">Пользователь не найден</response>
    [Authorize]
    [HttpGet("{personId}")]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, DataPerson>),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullProfile([FromRoute] string personId)
    {
        MetricsRegistry.EndpointRequestCounter
            .WithLabels("profile-by-id", "GET", "profile", Environment.MachineName).Inc();
        
        using (MetricsRegistry.EndpointDuration
                   .WithLabels("profile-by-id", "GET", "profile", Environment.MachineName)
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
    /// <response code="200">Успешно</response>
    /// <response code="403">Невалидный jwt токен</response>
    /// <response code="409">Тег занят</response>
    [Authorize]
    [HttpPut]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse<string, string>),StatusCodes.Status409Conflict)]
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