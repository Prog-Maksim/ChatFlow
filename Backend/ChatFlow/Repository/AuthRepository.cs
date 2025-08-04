using System.Text.Json;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ChatFlow.Repository;

public class AuthRepository: IAuthRepository
{
    private readonly ApplicationContext _context;
    private readonly IDatabase _database;
    private readonly ILogger<AuthRepository> _logger;
    
    private const int MaxAttempts = 5;
    private static readonly TimeSpan AttemptPeriod = TimeSpan.FromMinutes(1);

    public AuthRepository(ApplicationContext context, IConnectionMultiplexer connection, ILogger<AuthRepository> logger)
    {
        _context = context;
        _database = connection.GetDatabase();
        _logger = logger;
    }
    

    public async Task<Persons?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Persons.FirstOrDefaultAsync(p => p.NumberPhone == phoneNumber);
    }
    
    public async Task<Persons?> GetUserByIdAsync(string personId)
    {
        return await _context.Persons.FirstOrDefaultAsync(p => p.PersonId == personId);
    }

    public async Task<bool> AddUserDataAsync(DataPersons person)
    {
        await _context.DataPersons.AddAsync(person);
        return true;
    }

    public async Task<bool> AddUserAsync(Persons person)
    {
        await _context.Persons.AddAsync(person);
        return true;
    }

    public async Task<string> GenerateCodeAndSaveAsync(Persons personData, string userIpAddress)
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
        var redisValue = JsonSerializer.Serialize(totpData);
        
        await _database.StringSetAsync(redisKey, redisValue);
        return code;
    }

    public async Task<string> GenerateCodeAndSaveAsync(Persons personData, string userIpAdress, string totpCode)
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
        var redisValue = JsonSerializer.Serialize(totpData);
        
        await _database.StringSetAsync(redisKey, redisValue);
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
        
        if (!redisValue.HasValue || redisValue.IsNullOrEmpty)
            return null;
        
        var json = (string)redisValue!;
        if (string.IsNullOrWhiteSpace(json))
            return null;
        
        var data = JsonSerializer.Deserialize<TotpData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return data;
    }

    public async Task<bool> UpdateTotpDataByCodeAsync(string code, string totpCode)
    {
        var redisKey = $"TOTP:{code}";
        var redisValue = await _database.StringGetAsync(redisKey);
        
        if (!redisValue.HasValue || redisValue.IsNullOrEmpty)
            return false;
        
        var json = (string)redisValue!;
        if (string.IsNullOrWhiteSpace(json))
            return false;
        
        var data = JsonSerializer.Deserialize<TotpData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data is null)
        {
            _logger.LogWarning("Не удалось преобразовать структуру из Redis. Структура: {obj}", json);
            return false;
        }
        
        data.TotpCode = totpCode;
        
        var updatedRedisValue = JsonSerializer.Serialize(data);
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
        await _context.Sessions.Where(t => t.PersonId == personId && t.SessionId == sessionId).ExecuteUpdateAsync(s => s
            .SetProperty(
                r => r.IsRevoked,
                r => true)
            .SetProperty(
                r => r.RevokedAt,
                r => DateTime.UtcNow));
        
        var tag= "sessions";
        await _database.SetAddAsync(tag, sessionId);
        await _database.KeyExpireAsync(tag, TimeSpan.FromDays(JwtTokenService.RefreshTokenLifetimeDay));
    }
    
    public async Task AddSessionsToBanAsync(IEnumerable<string> sessionIds, string personId)
    {
        var tag = "sessions";
        
        var sessionsList = sessionIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        if (sessionsList.Count == 0)
            return;
        
        await _context.Sessions.Where(t => t.PersonId == personId && sessionsList.Contains(t.SessionId)).ExecuteUpdateAsync(s => s
            .SetProperty(
                r => r.IsRevoked,
                r => true)
            .SetProperty(
                r => r.RevokedAt,
                r => DateTime.UtcNow));

        RedisValue[] redisValues = sessionsList.Select(id => (RedisValue)id).ToArray();
        
        await _database.SetAddAsync(tag, redisValues);
        await _database.KeyExpireAsync(tag, TimeSpan.FromDays(JwtTokenService.RefreshTokenLifetimeDay));
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
        var attemptsValue = await _database.StringGetAsync(redisKey);

        if (attemptsValue.HasValue)
        {
            var attemptsStr = (string?)attemptsValue;
            if (!string.IsNullOrWhiteSpace(attemptsStr) && int.TryParse(attemptsStr, out var attempts) && attempts >= MaxAttempts)
                return true; // IP заблокирован
        }

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

    public async Task<bool> AddSessionAsync(Sessions session)
    {
        await _context.Sessions.AddAsync(session);
        return true;
    }

    public async Task<Sessions?> GetSessionByIdAsync(string personId, string sessionId)
    {
        return await _context.Sessions.FirstOrDefaultAsync(p => p.PersonId == personId && p.SessionId == sessionId);
    }

    public IQueryable<Sessions> GetSessionsAsync(string personId, bool state = false)
    {
        return _context.Sessions.Where(p => p.PersonId == personId && p.IsRevoked == state);
    }

    public async Task RevokeAllSessionsAsync(string personId)
    {
        await _context.Sessions
            .Where(p => p.PersonId == personId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                r => r.IsRevoked,
                r => true)
                .SetProperty(
                r => r.RevokedAt,
                r => DateTime.UtcNow));
    }
}