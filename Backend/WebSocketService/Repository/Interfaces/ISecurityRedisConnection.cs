using StackExchange.Redis;

namespace WebSocketService.Repository.Interfaces;

public interface ISecurityRedisConnection
{
    IConnectionMultiplexer Connection { get; }
}