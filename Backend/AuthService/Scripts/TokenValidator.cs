using AuthService.Models.Other;

namespace AuthService.Scripts;

public class TokenValidator
{
    private readonly JwtTokenService _jwtTokenService;

    public TokenValidator(JwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public async Task<(bool isValid, JwtTokenData? tokenData)> TryValidateTokenAsync(string token)
    {
        var tokenData = _jwtTokenService.GetJwtTokenData(token);
        if (await _jwtTokenService.ValidateJwtAccessToken(tokenData))
            return (true, tokenData);
        return (false, null);
    }
}