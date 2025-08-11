using System.Security.Cryptography;
using System.Text;
using ChatFlow.Scripts.Interfaces;

namespace ChatFlow.Scripts;

public class MessageEncryptionService: IMessageEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public MessageEncryptionService(byte[] key, byte[] iv)
    {
        _key = key;
        _iv = iv;
    }

    public string Encrypt(string message)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(message);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string message)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var cipherBytes = Convert.FromBase64String(message);
        var decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}