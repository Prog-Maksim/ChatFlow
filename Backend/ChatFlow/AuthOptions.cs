using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ChatFlow;

public class AuthOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string Key { get; set; } = null!;

    public SymmetricSecurityKey GetSymmetricSecurityKey()
        => new(Encoding.UTF8.GetBytes(Key));
}