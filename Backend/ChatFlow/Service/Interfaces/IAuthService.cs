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
    public Task<BaseResponse<string, RegistrationCode>> RegistrationUserAsync(RegistrationUser registrationUser,
        string userIpAddress);

    /// <summary>
    ///  Авторизация пользователя
    /// </summary>
    /// <param name="login">Номер телефона или почта</param>
    /// <param name="password">Пароль</param>
    /// <param name="userIpAddress">Ip адрес пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, RegistrationCode>> AuthorizationUserAsync(string login, string password,
        string userIpAddress);
}