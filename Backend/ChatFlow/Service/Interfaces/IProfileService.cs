using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface IProfileService
{
    /// <summary>
    /// Выдает краткую информацию о профиле
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, SummaryDataPerson>> GetSummaryProfileData(string accessToken,
        string? personId = null);

    /// <summary>
    /// Выдает полную информацию о профиле
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, DataPerson>> GetProfileData(string accessToken, string? personId = null);

    /// <summary>
    /// Обновляет информацию в профиле
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="profile">Данные профиля</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> UpdateProfileData(string accessToken, Profile profile);

    /// <summary>
    /// Возвращает все изображения пользователя
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, List<DataImage>>> GetProfileImages(string accessToken,
        string? personId = null);

    public Task<BaseResponse<string, List<PublicKeyResponse>>> GetPublicKeyAsync(string personId);
}