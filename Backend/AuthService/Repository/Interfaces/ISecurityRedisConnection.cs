using StackExchange.Redis;

namespace AuthService.Repository.Interfaces;

public interface ISecurityRedisConnection
{
    IConnectionMultiplexer Connection { get; }
}