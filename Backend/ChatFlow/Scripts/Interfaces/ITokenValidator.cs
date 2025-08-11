using ChatFlow.Models.Other;

namespace ChatFlow.Scripts.Interfaces;

public interface ITokenValidator
{
    Task<(bool isValid, JwtTokenData? tokenData)> TryValidateTokenAsync(string token);
}