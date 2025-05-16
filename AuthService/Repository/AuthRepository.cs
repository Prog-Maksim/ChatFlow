using AuthService.Models.DB;
using AuthService.Models.Other;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AuthService.Repository;

public class AuthRepository: IAuthRepository
{
    private readonly ApplicationContext _context;
    private readonly IDatabase _database;

    public AuthRepository(ApplicationContext context, IConnectionMultiplexer connection)
    {
        _context = context;
        _database = connection.GetDatabase();
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

    public bool UpdateUserAsync(Person person)
    {
        _context.Persons.Attach(person);
        _context.Entry(person).State = EntityState.Modified;

        return true;
    }

    public async Task<string> GenerateCodeAndSaveAsync(string personId, Person personData)
    {
        var random = new Random();
        var code = random.Next(10000000, 999999999).ToString();

        var totpData = new TotpData
        {
            PersonId = personId,
            PersonData = personData,
            TotpCode = null
        };
        
        var redisKey = $"TOTP:{code}";
        var redisValue = System.Text.Json.JsonSerializer.Serialize(totpData);
        
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

    public async Task AddJwtTokensToBanAsync(string personId, List<string> tokens)
    {
        var tag = $"ban:{personId}";

        if (tokens.Any())
        {
            await _database.SetAddAsync(tag, tokens.Select(t => (RedisValue)t).ToArray());
            await _database.KeyExpireAsync(tag, TimeSpan.FromDays(JwtTokenService.RefreshTokenLifetimeDay));
        }
    }

    public async Task AddJwtTokenToBanAsync(string personId, string token)
    {
        var tag = $"ban:{personId}";
        await _database.SetAddAsync(tag, token);
        await _database.KeyExpireAsync(tag, TimeSpan.FromDays(JwtTokenService.RefreshTokenLifetimeDay));
    }

    public async Task<bool> IsBannedTokenAsync(string personId, string token)
    {
        var tag = $"ban:{personId}";
        bool isBanned = await _database.SetContainsAsync(tag, token);
        return isBanned;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}