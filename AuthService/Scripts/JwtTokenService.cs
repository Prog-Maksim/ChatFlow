using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Enums;
using AuthService.Models.DB;
using AuthService.Models.Other;
using AuthService.Repository.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Scripts;

public class JwtTokenService
{
    private readonly AuthOptions _authOptions;
    private readonly IAuthRepository _authRepository;

    public JwtTokenService(AuthOptions authOptions, IAuthRepository authRepository)
    {
        _authOptions = authOptions;
        _authRepository = authRepository;
    }
    
    public const int AccessTokenLifetimeMinute = 5;
    public const int RefreshTokenLifetimeDay = 30;

    public string GenerateJwtAccessToken(string personId, string sessionId, int id)
    {        
        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, personId),
            new ("id", id.ToString()),
            new ("token_type", TokenType.AccessToken.ToString()),
            new ("session", sessionId),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var jwt = new JwtSecurityToken(
            issuer: _authOptions.Issuer,
            audience: _authOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(AccessTokenLifetimeMinute),
            signingCredentials: new SigningCredentials(_authOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public string GenerateJwtRefreshToken(string personId, int passwordVersion, string sessionId, int id)
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, personId),
            new ("id", id.ToString()),
            new ("token_type", TokenType.RefreshToken.ToString()),
            new ("session", sessionId),
            new ("version", passwordVersion.ToString()),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var jwt = new JwtSecurityToken(
            issuer: _authOptions.Issuer,
            audience: _authOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(RefreshTokenLifetimeDay),
            signingCredentials: new SigningCredentials(_authOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public Tokens CreateJwtToken(string personId, int passwordVersion, string sessionId, int id)
    {
        var accessToken = GenerateJwtAccessToken(personId, sessionId, id);
        var refreshToken = GenerateJwtRefreshToken(personId, passwordVersion, sessionId, id);

        return new Tokens { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public Tokens CreateJwtToken(string personId, int passwordVersion, string sessionId, int id, string oldRefreshToken)
    {
        _ = _authRepository.AddJwtTokenToBanAsync(personId, oldRefreshToken);
        
        var accessToken = GenerateJwtAccessToken(personId, sessionId, id);
        var refreshToken = GenerateJwtRefreshToken(personId, passwordVersion, sessionId, id);

        return new Tokens { AccessToken = accessToken, RefreshToken = refreshToken };
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

    /// <summary>
    /// Проверяет валидность токена
    /// </summary>
    /// <param name="token"></param>
    /// <param name="person"></param>
    /// <returns>true - токен валиден</returns>
    public async Task<bool> ValidateJwtRefreshToken(JwtTokenData token, Person person)
    {
        if (token.TokenType != TokenType.RefreshToken)
            return false;

        if (token.PasswordVersion != person.PasswordVersion)
            return false;

        return !await _authRepository.IsBannedTokenAsync(person.PersonId, token.Token, token.SessionId);
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
        
        return !await _authRepository.IsBannedTokenAsync(token.PersonId, token.Token, token.SessionId);
    }
}