using StackExchange.Redis;

namespace MessageService.Repository.Interfaces;

public interface ISecurityRedisConnection
{
    IConnectionMultiplexer Connection { get; }
}