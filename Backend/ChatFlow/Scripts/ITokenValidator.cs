using ChatFlow.Models.Other;

namespace ChatFlow.Scripts;

public interface ITokenValidator
{
    Task<(bool isValid, JwtTokenData? tokenData)> TryValidateTokenAsync(string token);
}