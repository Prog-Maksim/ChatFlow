using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MessageService.Enums;
using MessageService.Models.Other;
using StackExchange.Redis;

namespace MessageService.Service;

public class JwtTokenService
{
    private readonly IDatabase _database;
    public const int RefreshTokenLifetimeDay = 30;

    public JwtTokenService(IConnectionMultiplexer connection)
    {
        _database = connection.GetDatabase();
    }
    
    public JwtTokenData GetJwtTokenData(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        
        if (!handler.CanReadToken(token))
            throw new ArgumentException("Неверный jwt токен");
        
        var jwtToken = handler.ReadJwtToken(token);
        
        var userId = jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        var id = jwtToken.Claims.First(c => c.Type == "id").Value;
        var tokenType = jwtToken.Claims.First(c => c.Type == "token_type").Value;
        var sessionId = jwtToken.Claims.First(c => c.Type == "session").Value;
        var tokenTypeEnum = Enum.Parse<TokenType>(tokenType);
        var versionClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "version")?.Value;
        var jti = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        
        int version = 0;
        if(int.TryParse(versionClaim, out var data))
            version = data;

        int identificator = 0;
        if(int.TryParse(id, out var data1))
            identificator = data1;
        
        return new JwtTokenData
        {
            PersonId = userId,
            TokenType = tokenTypeEnum,
            PasswordVersion = version,
            SessionId = sessionId,
            Jti = jti,
            Id = identificator,
            Token = token
        };
    }

    public async Task RevokeSession(string sessionId)
    {
        var tag = "sessions";
        
        await _database.SetAddAsync(tag, sessionId);
        await _database.KeyExpireAsync(tag, TimeSpan.FromDays(RefreshTokenLifetimeDay));
    }
    
    /// <summary>
    /// Проверяет статус сессии
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>true - сессия не валидна</returns>
    public async Task<bool> IsBannedTokenAsync(string sessionId)
    {
        var tagSession = "sessions";
        bool isBannedSession = await _database.SetContainsAsync(tagSession, sessionId);
        return isBannedSession;
    }
    
    /// <summary>
    /// Проверяет валидность токена
    /// </summary>
    /// <param name="token"></param>
    /// <returns>true - токен валиден</returns>
    public async Task<bool> ValidateJwtAccessToken(JwtTokenData token)
    {
        if (token.TokenType != TokenType.AccessToken)
            return false;
        
        return !await IsBannedTokenAsync(token.SessionId);
    }
}