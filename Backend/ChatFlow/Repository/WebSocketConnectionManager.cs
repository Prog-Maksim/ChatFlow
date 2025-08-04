using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Other;
using ChatFlow.Monitoring;
using ChatFlow.Repository.Interfaces;

namespace ChatFlow.Repository;

public class WebSocketConnectionManager: IWebSocketConnectionManager
{
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _connections = new();

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
    }
    
    public void AddConnection(PersonRegion region, string personId, string sessionId, WebSocket socket)
    {
        MetricsRegistry.TotalActiveConnections
            .Inc();
        
        MetricsRegistry.ActiveConnectionsByGeo
            .WithLabels(region.Country, region.City, region.Latitude, region.Longitude)
            .Inc();

        if (!_connections.ContainsKey(personId))
            _connections[personId] = new ConcurrentDictionary<string, WebSocket>();
            
        var sessions = _connections[personId];
        if (!sessions.ContainsKey(sessionId))
            sessions[sessionId] = socket;
    }

    public async Task RemoveConnection(PersonRegion region, string personId, string sessionId)
    {
        if (_connections.TryGetValue(personId, out var sessions) && 
            sessions.TryRemove(sessionId, out var session))
        {
            if (sessions.IsEmpty)
                _connections.TryRemove(personId, out _);
    
            try
            {
                if (session.State == WebSocketState.Open || session.State == WebSocketState.CloseReceived)
                    await session.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
            catch (Exception error)
            {
                _logger.LogError(error, $"Не удалось закрыть соединение для пользователя: {personId}");
            }

            MetricsRegistry.TotalActiveConnections
                .Dec();
            
            MetricsRegistry.ActiveConnectionsByGeo
                .WithLabels(region.Country, region.City, region.Latitude, region.Longitude)
                .Inc();
        }
    }
    
    public async Task RemoveConnection(string personId, string sessionId)
    {
        if (_connections.TryGetValue(personId, out var sessions) && 
            sessions.TryRemove(sessionId, out var session))
        {
            if (sessions.IsEmpty)
                _connections.TryRemove(personId, out _);
    
            try
            {
                if (session.State == WebSocketState.Open || session.State == WebSocketState.CloseReceived)
                    await session.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
            catch (Exception error)
            {
                _logger.LogError(error, $"Не удалось закрыть соединение для пользователя: {personId}");
            }

            MetricsRegistry.TotalActiveConnections
                .Dec();
        }
    }

    public WebSocket? GetConnectionById(string personId, string sessionId)
    {
        return _connections.TryGetValue(personId, out var sessions)
            ? sessions.GetValueOrDefault(sessionId)
            : null;
    }
    
    public async Task SendMessageToUserAsync(string personId, MessageData message)
    {
        if (!_connections.TryGetValue(personId, out var sessions))
        {
            _logger.LogDebug($"Нет активных сессий для пользователя: {personId}");
            return;
        }

        var buffer = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            Type = "message",
            Data = message
        }));
        var segment = new ArraySegment<byte>(buffer);

        foreach (var (sessionId, socket) in sessions)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    _logger.LogDebug($"Сообщение отправлено пользователю {personId}, сессия {sessionId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при отправке сообщения пользователю {personId}, сессия {sessionId}");
                }
            }
            else
            {
                _logger.LogDebug($"Сессия {sessionId} пользователя {personId} не в состоянии Open");
            }
        }
    }

    public async Task SendMessageCreateChat(string chatId, string personId)
    {
        if (!_connections.TryGetValue(personId, out var sessions))
        {
            _logger.LogDebug($"Нет активных сессий для пользователя: {personId}");
            return;
        }

        var message = new
        {
            Type = "new-chat",
            ChatId = chatId
        };

        var buffer = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var segment = new ArraySegment<byte>(buffer);

        foreach (var (sessionId, socket) in sessions)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    _logger.LogDebug($"Сообщение отправлено пользователю {personId}, сессия {sessionId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при отправке сообщения пользователю {personId}, сессия {sessionId}");
                }
            }
            else
            {
                _logger.LogDebug($"Сессия {sessionId} пользователя {personId} не в состоянии Open");
            }
        }
    }
}