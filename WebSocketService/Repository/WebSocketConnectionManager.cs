using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebSocketService.Controllers;
using WebSocketService.Monitoring;
using WebSocketService.Repository.Interfaces;

namespace WebSocketService.Repository;

public class WebSocketConnectionManager: IWebSocketConnectionManager
{
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _connections = new();

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
    }
    
    public void AddConnection(string personId, string sessionId, WebSocket socket)
    {
        MetricsRegistry.TotalActiveConnections
            .WithLabels("web-socket", Environment.MachineName)
            .Inc();


        if (!_connections.ContainsKey(personId))
            _connections[personId] = new ConcurrentDictionary<string, WebSocket>();
            
        var sessions = _connections[personId];
        if (!sessions.ContainsKey(sessionId))
            sessions[sessionId] = socket;
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
                .WithLabels("web-socket", Environment.MachineName)
                .Dec();
        }
    }

    public WebSocket? GetConnectionById(string personId, string sessionId)
    {
        return _connections.TryGetValue(personId, out var sessions)
            ? sessions.GetValueOrDefault(sessionId)
            : null;
    }
}