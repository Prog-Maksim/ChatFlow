using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebSocketService;

public class AuthOptions
{
    public string Issuer { get; }
    public string Audience { get; }
    private string Key { get; }

    public AuthOptions(IConfiguration configuration)
    {
        var authSection = configuration.GetSection("Auth");
        Issuer = authSection["Issuer"] ?? throw new InvalidOperationException("Issuer is not set in configuration.");
        Audience = authSection["Audience"] ?? throw new InvalidOperationException("Audience is not set in configuration.");
        Key = authSection["Key"] ?? throw new InvalidOperationException("JWT Key is not set in configuration.");
    }

    public SymmetricSecurityKey GetSymmetricSecurityKey() => new(Encoding.UTF8.GetBytes(Key));
}