using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Регистрирует нового пользователя
    /// </summary>
    /// <param name="registrationUser">Данные о пользователе</param>
    /// <param name="userIpAddress">Ip адрес пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> RegistrationUserAsync(RegistrationUser registrationUser,
        string userIpAddress);

    /// <summary>
    ///  Авторизация пользователя
    /// </summary>
    /// <param name="login">Номер телефона или почта</param>
    /// <param name="password">Пароль</param>
    /// <param name="userIpAddress">Ip адрес пользователя</param>
    /// <param name="publicKey">Публичный ключ пользователя</param>
    /// <param name="userAgent"></param>
    /// <param name="refreshToken">Refresh токен</param>
    /// <returns></returns>
    public Task<BaseResponse<string, AuthTokens>> AuthorizationUserAsync(string login, string password, string userIpAddress, string publicKey, string userAgent, string? refreshToken = null);
}