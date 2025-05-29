using System.Text.Json;
using SearchService.Repository.Interfaces;
using StackExchange.Redis;

namespace SearchService.Service;

public class TokenSubscriberService: BackgroundService
{
    private readonly ISubscriber _subscriber;
    private readonly ILogger<TokenSubscriberService> _logger;
    private readonly JwtTokenService _jwtTokenService;

    public TokenSubscriberService(ISecurityRedisConnection securityRedis, ILogger<TokenSubscriberService> logger, JwtTokenService jwtTokenService)
    {
        _subscriber = securityRedis.Connection.GetSubscriber();
        _logger = logger;
        _jwtTokenService = jwtTokenService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Подключен к Redis pub/sub");
        
        await _subscriber.SubscribeAsync("token-revoked", (channel, message) =>
        {
            var data = JsonSerializer.Deserialize<TokenRevokedMessage>(message);
            if (data is not null)
            {
                Console.WriteLine($"[TOKEN INVALIDATED] TokenId: {data.SessionId}, RevokedAt: {data.RevokedAt}");
                _ = _jwtTokenService.RevokeSession(data.SessionId);
            }
            else
                _logger.LogWarning("Не удалось преобразовать сообщение: {@message}", message);
        });
        
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Redis pub/sub отключен");
        }
    }
    
    
    private class TokenRevokedMessage
    {
        public required string SessionId { get; set; }
        public DateTime RevokedAt { get; set; }
    }
}