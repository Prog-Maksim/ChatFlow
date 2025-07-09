using System.Text.Json;
using AuthService.Repository.Interfaces;
using StackExchange.Redis;

namespace AuthService.Service;

public class TokenPublisherService
{
    private readonly ISubscriber _subscriber;

    public TokenPublisherService(ISecurityRedisConnection redisConnection)
    {
        _subscriber = redisConnection.Connection.GetSubscriber();
    }

    /// <summary>
    /// Публикует сообщение об отозванных токенах
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="personId">Идентификатор пользователя</param>
    public async Task PublishTokenRevokedAsync(string sessionId, string personId)
    {
        var message = JsonSerializer.Serialize(new
        {
            PersonId = personId,
            SessionId = sessionId,
            RevokedAt = DateTime.UtcNow
        });

        await _subscriber.PublishAsync(RedisChannel.Literal("token-revoked"), message);
    }
    
    /// <summary>
    /// Публикует сообщение об отозванных токенах
    /// </summary>
    /// <param name="sessionIds">Идентификаторы сессий</param>
    /// <param name="personId">Идентификатор пользователя</param>
    public async Task PublishTokenRevokedAsync(IEnumerable<string> sessionIds, string personId)
    {
        var tasks = sessionIds.Select(sessionId =>
        {
            var message = JsonSerializer.Serialize(new
            {
                PersonId = personId,
                SessionId = sessionId,
                RevokedAt = DateTime.UtcNow
            });

            return _subscriber.PublishAsync(RedisChannel.Literal("token-revoked"), message);
        });

        await Task.WhenAll(tasks);
    }
}