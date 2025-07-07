using System.Net.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Services;
using WebSocketService.Models.Response;

namespace WebSocketService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("backend-f/v{version:apiVersion}/ws")]
public class WebSocketController(ILogger<WebSocketController> logger, Service.WebSocketService service): ControllerBase
{
    /// <summary>
    /// Подключение пользователей по WebSocket
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// - 400: Запрос не является WebSocket  
    /// - 401: Неавторизованный пользователь  
    /// - 406: Не удалось определить IP-адрес  
    /// </remarks>
    [Authorize]
    [HttpGet]
    public async Task Connect()
    {
        string? userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();

        if (userIpAddress is null)
        {
            HttpContext.Response.StatusCode = 406;
            await HttpContext.Response.WriteAsync("Невозможно определить ip адрес");
            return;
        }
        
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader.Substring("Bearer ".Length);
        
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Запрос не является Web Socket");
            return;
        }
          
        WebSocket ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var response = await service.ConnectPersonAsync(userIpAddress, token, ws);
        
        if (!response.Successfully)
            await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, response.Message, CancellationToken.None);
    }
}