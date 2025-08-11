using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatFlow.Enums;
using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Other;
using ChatFlow.Monitoring;
using ChatFlow.Repository.Interfaces;
using ChatUser = ChatFlow.Models.DB.ChatUser;

namespace ChatFlow.Repository;

public class WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger) : IWebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _connections = new();
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

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
                logger.LogError(error, $"Не удалось закрыть соединение для пользователя: {personId}");
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
                logger.LogError(error, $"Не удалось закрыть соединение для пользователя: {personId}");
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


    public async Task SendMessageToUserAsync(MessageData message, List<ChatUser> persons)
    {
        var payload = new WsMessage<MessageData>("message", message);
        await SendToPersonsAsync(persons.Select(p => p.PersonId), payload);
    }

    public async Task SendMessageDeleteMessageAsync(MessageData message, List<ChatUser> persons)
    {
        var payload = new WsMessage<DeleteMessagePayload>(
            "message-delete",
            new DeleteMessagePayload(message.ChatId, message.MessageId)
        );
        await SendToPersonsAsync(persons.Select(p => p.PersonId), payload);
    }

    public async Task SendMessageCreateChat(string chatId, string personId)
    {
        var payload = new WsMessage<string>("new-chat", chatId);
        await SendToPersonAsync(personId, payload);
    }

    public async Task SendMessageMigrationChat(string oldChatId, string newChatId, ChatDocument chatData)
    {
        var payload = new WsMessage<MigrationChatPayload>("migration-chat", new MigrationChatPayload(oldChatId, newChatId, chatData.Type));
        await SendToPersonsAsync(chatData.Persons.Select(p => p.PersonId), payload);
    }

    public async Task SendMessageDeleteHistoryChat(string chatId, List<ChatUser> persons)
    {
        var payload = new WsMessage<string>("delete-history-chat", chatId);
        await SendToPersonsAsync(persons.Select(p => p.PersonId), payload);
    }

    public async Task SendMessageViewMessage(string chatId, string messageId, ChatUser person)
    {
        var payload = new WsMessage<ViewMessage>("view-message", new ViewMessage(chatId, messageId));
        await SendToPersonAsync(person.PersonId, payload);
    }

    public async Task SendMessageDeleteChat(string chatId, List<ChatUser> persons)
    {
        var payload = new WsMessage<string>("delete-chat", chatId);
        await SendToPersonsAsync(persons.Select(p => p.PersonId), payload);
    }
    
    
    private Task SendToPersonsAsync(IEnumerable<string> personIds, object payload)
    {
        var tasks = personIds.Select(personId => SendToPersonAsync(personId, payload));
        return Task.WhenAll(tasks);
    }
    private async Task SendToPersonAsync(string personId, object payload)
    {
        if (!_connections.TryGetValue(personId, out var sessions))
        {
            logger.LogDebug($"Нет активных сессий для пользователя: {personId}");
            return;
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(buffer);

        foreach (var (sessionId, socket) in sessions)
        {
            if (socket.State != WebSocketState.Open)
            {
                logger.LogDebug($"Сессия {sessionId} пользователя {personId} не в состоянии Open");
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                logger.LogDebug($"Сообщение отправлено пользователю {personId}, сессия {sessionId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Ошибка при отправке сообщения пользователю {personId}, сессия {sessionId}");
            }
        }
    }
    
    
    private record WsMessage<T>(string Type, T Data);
    private record DeleteMessagePayload(string ChatId, string MessageId);
    private record MigrationChatPayload(string OldChatId, string NewChatId, ChatType NewChatType);
    private record ViewMessage(string ChatId, string MessageId);
}