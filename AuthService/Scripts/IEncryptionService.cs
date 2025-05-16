namespace AuthService.Scripts;

public interface IEncryptionService
{
    /// <summary>
    /// Шифрует данные
    /// </summary>
    /// <param name="text">Текст для шифрования</param>
    /// <returns></returns>
    public string Encrypt(string text);
    
    /// <summary>
    /// Расшифровывает данные
    /// </summary>
    /// <param name="encryptedText">Текст для расшифровки</param>
    /// <returns></returns>
    public string Decrypt(string encryptedText);
}