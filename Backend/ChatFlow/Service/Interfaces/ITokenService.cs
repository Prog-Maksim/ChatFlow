using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Обновляет Refresh токен
    /// </summary>
    /// <param name="refreshToken">Refresh токен</param>
    /// <returns></returns>
    public Task<BaseResponse<string, AuthTokens>> RefreshAccessToken(string refreshToken);
}