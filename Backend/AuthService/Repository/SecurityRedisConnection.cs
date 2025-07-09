using AuthService.Repository.Interfaces;
using StackExchange.Redis;

namespace AuthService.Repository;

public class SecurityRedisConnection: ISecurityRedisConnection
{
    public IConnectionMultiplexer Connection { get; }

    public SecurityRedisConnection(IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("RedisSecurity");
        
        if (string.IsNullOrEmpty(connStr))
            throw new NullReferenceException("RedisSecurity connection string is null or empty");
        
        Connection = ConnectionMultiplexer.Connect(connStr);
    }
}