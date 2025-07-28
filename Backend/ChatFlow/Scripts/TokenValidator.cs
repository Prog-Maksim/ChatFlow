using ChatFlow.Models.Other;

namespace ChatFlow.Scripts;

public class TokenValidator: ITokenValidator
{
    private readonly IJwtTokenService _jwtTokenService;

    public TokenValidator(IJwtTokenService jwtTokenService)
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