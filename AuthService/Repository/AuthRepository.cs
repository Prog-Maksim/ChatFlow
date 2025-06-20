using AuthService.Models.DB;
using AuthService.Models.Other;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using AuthService.Service;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AuthService.Repository;

public class AuthRepository: IAuthRepository
{
    private readonly ApplicationContext _context;
    private readonly TokenPublisherService _tokenPublisherService;
    private readonly IDatabase _database;
    
    private const int MaxAttempts = 5;
    private static readonly TimeSpan AttemptPeriod = TimeSpan.FromMinutes(1);

    public const int CodeLifetimeMinute = 15;

    public AuthRepository(ApplicationContext context, IConnectionMultiplexer connection, TokenPublisherService tokenPublisherService)
    {
        _context = context;
        _database = connection.GetDatabase();
        _tokenPublisherService = tokenPublisherService;
    }
    

    public async Task<Person?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Persons.FirstOrDefaultAsync(p => p.NumberPhone == phoneNumber);
    }
    
    public async Task<Person?> GetUserByIdAsync(string personId)
    {
        return await _context.Persons.FirstOrDefaultAsync(p => p.PersonId == personId);
    }

    public async Task<bool> AddUserAsync(Person person)
    {
        await _context.Persons.AddAsync(person);
        return true;
    }

    public async Task<string> GenerateCodeAndSaveAsync(Person personData, string userIpAddress)
    {
        var random = new Random();
        var code = random.Next(10000000, 999999999).ToString();

        var totpData = new TotpData
        {
            PersonId = personData.PersonId,
            PersonData = personData,
            IpAddress = userIpAddress,
            TotpCode = null,
            IsUpdate = true,
            IsRead = true
        };
        
        var redisKey = $"TOTP:{code}";
        var redisValue = System.Text.Json.JsonSerializer.Serialize(totpData);
        
        await _database.StringSetAsync(redisKey, redisValue);
        await _database.KeyExpireAsync(redisKey, TimeSpan.FromMinutes(CodeLifetimeMinute));
        return code;
    }

    public async Task<string> GenerateCodeAndSaveAsync(Person personData, string userIpAdress, string totpCode)
    {
        var random = new Random();
        var code = random.Next(10000000, 999999999).ToString();

        var totpData = new TotpData
        {
            PersonId = personData.PersonId,
            PersonData = personData,
            IpAddress = userIpAdress,
            TotpCode = totpCode,
            IsUpdate = false,
            IsRead = false
        };
        
        var redisKey = $"TOTP:{code}";
        var redisValue = System.Text.Json.JsonSerializer.Serialize(totpData);
        
        await _database.StringSetAsync(redisKey, redisValue);
        await _database.KeyExpireAsync(redisKey, TimeSpan.FromMinutes(CodeLifetimeMinute));
        return code;
    }

    public async Task<bool> CheckCodeAsync(string code)
    {
        var redisKey = $"TOTP:{code}";
        var result = await _database.KeyExistsAsync(redisKey);
        return result;
    }

    public async Task<TotpData?> GetTotpDataByCodeAsync(string code)
    {
        var redisKey = $"TOTP:{code}";

        var redisValue = await _database.StringGetAsync(redisKey);
        
        if (!redisValue.HasValue)
            return null;
        
        var data = System.Text.Json.JsonSerializer.Deserialize<TotpData>(redisValue);
        return data;
    }

    public async Task<bool> UpdateTotpDataByCodeAsync(string code, string totpCode)
    {
        var redisKey = $"TOTP:{code}";

        var redisValue = await _database.StringGetAsync(redisKey);
        
        if (!redisValue.HasValue)
            return false;

        
        var data = System.Text.Json.JsonSerializer.Deserialize<TotpData>(redisValue);
        data.TotpCode = totpCode;
        
        var updatedRedisValue = System.Text.Json.JsonSerializer.Serialize(data);
        await _database.StringSetAsync(redisKey, updatedRedisValue);

        return true;
    }
    
    public async Task DeleteTotpDataByCodeAsync(string code)
    {
        var redisKey = $"TOTP:{code}";
        await _database.KeyDeleteAsync(redisKey);
    }

    public async Task AddJwtTokenToBanAsync(string personId, string token)
    {
        var tag = $"ban:{personId}";
        await _database.SetAddAsync(tag, token);
        await _database.KeyExpireAsync(tag, TimeSpan.FromDays(JwtTokenService.RefreshTokenLifetimeDay));
    }

    public async Task AddSessionToBanAsync(string sessionId, string personId)
    {
        var tag= "sessions";
        await _database.SetAddAsync(tag, sessionId);
        await _database.KeyExpireAsync(tag, TimeSpan.FromDays(JwtTokenService.RefreshTokenLifetimeDay));
        await _tokenPublisherService.PublishTokenRevokedAsync(sessionId, personId);
    }
    
    public async Task AddSessionsToBanAsync(IEnumerable<string> sessionIds, string personId)
    {
        var tag = "sessions";
        
        var sessionsList = sessionIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        if (sessionsList.Count == 0)
            return;

        RedisValue[] redisValues = sessionsList.Select(id => (RedisValue)id).ToArray();
        
        await _database.SetAddAsync(tag, redisValues);
        await _database.KeyExpireAsync(tag, TimeSpan.FromDays(JwtTokenService.RefreshTokenLifetimeDay));
        await _tokenPublisherService.PublishTokenRevokedAsync(sessionsList, personId);
    }


    public async Task<bool> IsBannedTokenAsync(string personId, string token, string sessionId)
    {
        var tag = $"ban:{personId}";
        var tagSession = "sessions";
        bool isBanned = await _database.SetContainsAsync(tag, token);
        bool isBannedSession = await _database.SetContainsAsync(tagSession, sessionId);
        return isBanned || isBannedSession;
    }
    
    public async Task<bool> IsBlockedAsync(string ip)
    {
        string redisKey = $"login_attempts:{ip}";

        var attempts = await _database.StringGetAsync(redisKey);

        if (attempts.HasValue && int.Parse(attempts) >= MaxAttempts)
            return true; // Заблокировать IP

        return false;
    }

    public async Task IncrementLoginAttemptsAsync(string ip)
    {
        string redisKey = $"login_attempts:{ip}";
        var newCount = await _database.StringIncrementAsync(redisKey);

        if (newCount == 1)
            await _database.KeyExpireAsync(redisKey, AttemptPeriod);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetNumberSessionsAsync(string personId)
    {
        var sessions = await _context.Sessions.Where(p => p.PersonId == personId && p.IsRevoked == false).ToListAsync();
        return sessions.Count;
    }

    public async Task<bool> AddSessionAsync(Session session)
    {
        await _context.Sessions.AddAsync(session);
        return true;
    }

    public async Task<Session?> GetSessionByIdAsync(string personId, string sessionId)
    {
        return await _context.Sessions.FirstOrDefaultAsync(p => p.PersonId == personId && p.SessionId == sessionId);
    }

    public IQueryable<Session> GetSessionsAsync(string personId, bool state = false)
    {
        return _context.Sessions.Where(p => p.PersonId == personId && p.IsRevoked == state);
    }

    public async Task RevokeAllSessionsAsync(string personId)
    {
        await _context.Sessions
            .Where(p => p.PersonId == personId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                r => r.IsRevoked,
                r => r.IsRevoked == true));
    }
}