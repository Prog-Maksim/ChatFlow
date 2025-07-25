using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface ITwoFactorService
{
    /// <summary>
    /// Создает Secret для добавления в GoogleAuthenticator
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <param name="userIpAddress">IP адрес пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, Token2Fa>> AddGoogleAuthenticatorAsync(string code, string userIpAddress);

    /// <summary>
    /// Проверяет код и выдает токены
    /// </summary>
    /// <param name="code">Код создания</param>
    /// <param name="key">Код из Google Authenticator</param>
    /// <param name="userIpAddress">IP адрес пользователя</param>
    /// <param name="userAgent">user agent пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, AuthTokens>> CheckGoogleAuthenticatorAsync(string code, string key,
        string userIpAddress, string userAgent);

    /// <summary>
    /// Создает Qr-code для добавления в Google Authenticator
    /// </summary>
    /// <param name="code"></param>
    /// <param name="userIpAddress">IP адрес пользователя</param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public Task<byte[]> GetQrCodeGoogleAuthenticatorAsync(string code, string userIpAddress);
}