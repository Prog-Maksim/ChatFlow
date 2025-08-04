namespace ChatFlow.Scripts;

public interface IMessageEncryptionService
{
    /// <summary>
    /// Шифрование сообщения
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns></returns>
    public string Encrypt(string message);

    /// <summary>
    /// Расшифровка сообщения
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns></returns>
    public string Decrypt(string message);
}