using OtpNet;
using QRCoder;

namespace AuthService.Scripts;

public static class AuthenticatorService
{
    /// <summary>
    /// Генерирует ключ для пользователя
    /// </summary>
    /// <returns>20-байтовый ключ</returns>
    public static string GenerateKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Key = Base32Encoding.ToString(key);
        
        return base32Key;
    }
    
    /// <summary>
    /// Проверяет код
    /// </summary>
    /// <param name="code">Код от пользователя</param>
    /// <param name="secret">Пользовательский ключ</param>
    /// <returns>true - код верен</returns>
    public static bool CheckValidKey(string code, string secret)
    {
        Totp totp = new Totp(Base32Encoding.ToBytes(secret));
        
        bool isValid = totp.VerifyTotp(code, out long _, VerificationWindow.RfcSpecifiedNetworkDelay);
        return isValid;
    }

    /// <summary>
    /// Генерирует url для входа в Google Authenticator
    /// </summary>
    /// <param name="secret">Пользовательский ключ</param>
    /// <param name="user">Данные пользователя</param>
    /// <returns>Url для пользователя</returns>
    public static string GenerateUrl(string secret, string user)
    {
        string service = "ChatFlow";
        string qrUri = $"otpauth://totp/{Uri.EscapeDataString(service)}:{Uri.EscapeDataString(user)}" +
                       $"?secret={secret}&issuer={Uri.EscapeDataString(service)}&algorithm=SHA1&digits=6&period=30";
        
        return qrUri;
    }
    
    /// <summary>
    /// Генерирует Qr-code
    /// </summary>
    /// <param name="url">Url в Google Authenticator</param>
    /// <returns>Массив байтов</returns>
    public static byte[] GenerateQrCode(string url)
    {
        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
        using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
        using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
        {
            byte[] qrCodeImage = qrCode.GetGraphic(20);
            return qrCodeImage;
        }
    }
}