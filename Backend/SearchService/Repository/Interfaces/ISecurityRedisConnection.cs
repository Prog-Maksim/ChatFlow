using StackExchange.Redis;

namespace SearchService.Repository.Interfaces;

public interface ISecurityRedisConnection
{
    IConnectionMultiplexer Connection { get; }
}