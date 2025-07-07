using StackExchange.Redis;
using WebSocketService.Repository.Interfaces;

namespace WebSocketService.Repository;

public class SecurityRedisConnection: ISecurityRedisConnection
{
    public IConnectionMultiplexer Connection { get; }

    public SecurityRedisConnection(IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("RedisSecurity");
        Connection = ConnectionMultiplexer.Connect(connStr);
    }
}