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

    public string GenerateJwtAccessToken(string personId)
    {        
        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, personId),
            new ("token_type", TokenType.AccessToken.ToString()),
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

    public string GenerateJwtRefreshToken(string personId, int passwordVersion)
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, personId),
            new ("token_type", TokenType.RefreshToken.ToString()),
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

    public Tokens CreateJwtToken(string personId, int passwordVersion)
    {
        var accessToken = GenerateJwtAccessToken(personId);
        var refreshToken = GenerateJwtRefreshToken(personId, passwordVersion);

        return new Tokens { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public Tokens CreateJwtToken(string personId, int passwordVersion, string oldRefreshToken)
    {
        _ = _authRepository.AddJwtTokenToBanAsync(personId, oldRefreshToken);
        
        var accessToken = GenerateJwtAccessToken(personId);
        var refreshToken = GenerateJwtRefreshToken(personId, passwordVersion);

        return new Tokens { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public JwtTokenData GetJwtTokenData(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        
        if (!handler.CanReadToken(token))
            throw new ArgumentException("Неверный jwt токен");
        
        var jwtToken = handler.ReadJwtToken(token);
        
        var userId = jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        var tokenType = jwtToken.Claims.First(c => c.Type == "token_type").Value;
        var tokenTypeEnum = Enum.Parse<TokenType>(tokenType);
        var versionClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "version")?.Value;
        var jti = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        
        int version = 0;
        if (int.TryParse(versionClaim, out var parsedVersion))
            version = parsedVersion;
        
        return new JwtTokenData
        {
            PersonId = userId,
            TokenType = tokenTypeEnum,
            PasswordVersion = version,
            Jti = jti,
            Token = token
        };
    }

    public async Task<bool> ValidateJwtRefreshToken(JwtTokenData token, Person person)
    {
        if (token.TokenType != TokenType.RefreshToken)
            return false;

        if (token.PasswordVersion != person.PasswordVersion)
            return false;

        return !await _authRepository.IsBannedTokenAsync(person.PersonId, token.Token);
    }
}