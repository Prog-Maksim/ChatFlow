using ProfileService.Repository.Interfaces;
using StackExchange.Redis;

namespace ProfileService.Repository;

public class SecurityRedisConnection: ISecurityRedisConnection
{
    public IConnectionMultiplexer Connection { get; }

    public SecurityRedisConnection(IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("RedisSecurity");
        Connection = ConnectionMultiplexer.Connect(connStr);
    }
}