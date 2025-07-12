using System.Net.WebSockets;
using WebSocketService.Enums;
using WebSocketService.Models.Other;
using WebSocketService.Models.Response;
using WebSocketService.Repository.Interfaces;
using WebSocketService.Scripts;

namespace WebSocketService.Service;

public class WebSocketService
{
    private readonly ILogger<WebSocketService> _logger;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IWebSocketConnectionManager _connectionManager;

    public WebSocketService(IWebSocketConnectionManager connectionManager, ILogger<WebSocketService> logger, JwtTokenService jwtTokenService)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<BaseResponse<string, string>> ConnectPersonAsync(string userIpAdress, string accessToken, WebSocket socket)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, string> 
                { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (socket.State != WebSocketState.Open)
            return new BaseResponse<string, string>
                { Message = "Соединение должно быть открытым", Type = ResponseType.WebSocketIsNotOpen, Successfully = false, Status = 400, Errors = "Bad Request", Data = null };
        
        PersonRegion region = await DeterminingIpAddress.GetPositionUser(userIpAdress);
        
        _connectionManager.AddConnection(region, dataToken.PersonId, dataToken.SessionId, socket);
        _logger.LogInformation("Подключен новый пользователь");
        await ListenAndHoldOpenConnectionAsync(region, dataToken.PersonId, dataToken.SessionId, socket);
        return new BaseResponse<string, string> 
            { Message = "Соединение закрыто", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null, Data = null };
    }

    public async Task ListenAndHoldOpenConnectionAsync(PersonRegion region, string personId, string sessionId, WebSocket ws)
    
    {
        var buffer = new byte[1];

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch (WebSocketException ex)
        {
            if (ex.Message.Contains("closed the WebSocket connection without completing the close handshake"))
                _logger.LogInformation($"Клиент {personId} отключился некорректно (без полного закрытия соединения).");
            else
                _logger.LogError(ex, "Ошибка WebSocket");
        }
        finally
        {
            await _connectionManager.RemoveConnection(region, personId, sessionId);
        }
    }
}