namespace ChatFlow.Scripts.Interfaces;

public interface IHmacService
{
    /// <summary>
    /// Генерация Hmac на основе зашифрованного сообщения
    /// </summary>
    /// <param name="message">Зашифрованное сообщение</param>
    /// <returns>Hmac код</returns>
    public string ComputeHmac(string message);

    /// <summary>
    /// Проверка Hmac
    /// </summary>
    /// <param name="message">Зашифрованное сообщение</param>
    /// <param name="hmacToCheck">Код Hmac</param>
    /// <returns>Результат проверки</returns>
    public bool VerifyHmac(string message, string hmacToCheck);
}