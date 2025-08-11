using System.Security.Cryptography;

namespace ChatFlow.Service.Other;

public class AuthValidator
{
    /// <summary>
    /// Проверяет переданный ключ
    /// </summary>
    /// <param name="base64Key">Ключ</param>
    /// <returns></returns>
    public static bool IsValidRsaPublicKey(string base64Key)
    {
        try
        {
            byte[] keyBytes = Convert.FromBase64String(base64Key);
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(keyBytes, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Проверяет что ключ приватный
    /// </summary>
    /// <param name="base64Key">Ключ</param>
    /// <returns></returns>
    public static bool IsPrivateKey(string base64Key)
    {
        try
        {
            byte[] keyBytes = Convert.FromBase64String(base64Key);
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(keyBytes, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}