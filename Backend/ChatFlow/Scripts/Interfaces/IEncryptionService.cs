namespace ChatFlow.Scripts.Interfaces;

public interface IEncryptionService
{
    /// <summary>
    /// Шифрует текст
    /// </summary>
    /// <param name="text">Текст для шифрования</param>
    /// <returns>Зашифрованный текст</returns>
    public string Encrypt(string text);
    
    /// <summary>
    /// Расшифровывает текст
    /// </summary>
    /// <param name="text">Текст для расшифровки</param>
    /// <returns>Расшифрованный текст</returns>
    public string Decrypt(string text);
}