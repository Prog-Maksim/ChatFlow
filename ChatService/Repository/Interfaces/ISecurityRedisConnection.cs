using StackExchange.Redis;

namespace ChatService.Repository.Interfaces;

public interface ISecurityRedisConnection
{
    IConnectionMultiplexer Connection { get; }
}