using System.Security.Cryptography;
using System.Text;

namespace ChatFlow.Scripts;

public class HmacService: IHmacService
{
    private readonly byte[] _hmacKey;

    public HmacService(byte[] hmacKey)
    {
        _hmacKey = hmacKey;
    }

    public string ComputeHmac(string message)
    {
        using var hmac = new HMACSHA256(_hmacKey);
        var bytes = Encoding.UTF8.GetBytes(message);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyHmac(string message, string hmacToCheck)
    {
        var computed = ComputeHmac(message);
        return computed == hmacToCheck;
    }
}