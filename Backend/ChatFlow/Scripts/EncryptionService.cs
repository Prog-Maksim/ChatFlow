using System.Security.Cryptography;
using System.Text;

namespace ChatFlow.Scripts;

public class EncryptionService: IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        string? keyString = configuration["Encryption:Key"];
        if (string.IsNullOrEmpty(keyString) || keyString.Length != 32)
            throw new ArgumentException("Encryption key must be 32 characters long.");
        
        _key = Encoding.UTF8.GetBytes(keyString);
    }
    
    public string Encrypt(string text)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;
            aes.GenerateIV();

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(text);
                    cs.Write(inputBytes, 0, inputBytes.Length);
                    cs.FlushFinalBlock();
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    
    public string Decrypt(string text)
    {
        byte[] encryptedBytes = Convert.FromBase64String(text);

        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;

            using (MemoryStream ms = new MemoryStream(encryptedBytes))
            {
                byte[] iv = new byte[16];
                ms.ReadExactly(iv, 0, iv.Length);
                aes.IV = iv;

                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader reader = new StreamReader(cs, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}