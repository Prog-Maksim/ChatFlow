using Microsoft.EntityFrameworkCore;

namespace API.Scripts;

public class RefreshTokenService
{
    public static string GenerateRefreshToken()
    {
        var refreshToken = Guid.NewGuid().ToString();
        return refreshToken;
    }

    public static async Task<bool> ValidateRefreshToken(string userId, string refreshToken, ApplicationContext context)
    {
        var sessions = await context.session.Where(s => s.PersonId == userId).ToListAsync();
        return sessions.Any(s => s.SessionToken == refreshToken);
    }
}