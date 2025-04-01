using System.ComponentModel.DataAnnotations;
using AuthService.Models.Requests;
using AuthService.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class AuthController(ILogger<AuthController> logger, Service.AuthService authService): ControllerBase
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="registrationUser"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("registration")]
    public async Task<IActionResult> RegistrationUser([FromBody][Required] RegistrationUser registrationUser)
    {
        string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
        if (userIpAddress == null)
        {
            var error = new BaseResponse
            {
                Message = "Невозможно определить ip адрес",
                Success = false,
                StatusCode = 400,
                Error = "Bad Request"
            };
            logger.LogError("Невозможно определить ip адрес");
            return StatusCode(error.StatusCode, error);
        }
        
        var response = await authService.RegistrationUserAsync(registrationUser, userIpAddress);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("add-google-authenticator")]
    public async Task<IActionResult> CreateGoogleAuthenticator(string code)
    {
        var response =  await authService.AddGoogleAuthenticatorAsync(code);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("check-code")]
    public async Task<IActionResult> CheckCode(string code, string key)
    {
        var response = await authService.CheckGoogleAuthenticatorAsync(code, key);
        
        if (!response.Success)
            return StatusCode(response.StatusCode, response);
        
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("qr-code")]
    public async Task<IActionResult> GetQrCode(string code)
    {
        var response = await authService.GetQrCodeGoogleAuthenticatorAsync(code);
        return File(response, "image/png");
    }
}