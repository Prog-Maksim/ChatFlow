namespace API;

public class RefreshTokenService
{
    private static Dictionary<string, string> _refreshTokens = new();

    public static string GenerateRefreshToken(string userId)
    {
        var refreshToken = Guid.NewGuid().ToString();
        _refreshTokens[userId] = refreshToken;
        return refreshToken;
    }

    public static bool ValidateRefreshToken(string userId, string refreshToken)
    {
        return _refreshTokens.TryGetValue(userId, out var storedToken) && storedToken == refreshToken;
    }
}