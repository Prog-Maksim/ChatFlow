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

    public async Task PublishTokenRevokedAsync(string sessionId, string personId)
    {
        var message = JsonSerializer.Serialize(new
        {
            PersonId = personId,
            SessionId = sessionId,
            RevokedAt = DateTime.UtcNow
        });

        await _subscriber.PublishAsync("token-revoked", message);
    }
    
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

            return _subscriber.PublishAsync("token-revoked", message);
        });

        await Task.WhenAll(tasks);
    }
}