using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface IImageService
{
    /// <summary>
    /// Сохраняет изображение в профиле
    /// </summary>
    /// <param name="file">Загружаемый файл</param>
    /// <param name="accessToken">Access токен</param>
    /// <param name="top">Отступ сверху</param>
    /// <param name="left">Отступ слева</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> UploadFile(IFormFile file, string accessToken, double? top = 0,
        double? left = 0);

    /// <summary>
    /// Выдает кол-во фотографий у пользователя
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, CountImage>> GetCountImages(string accessToken, string? personId = null);

    /// <summary>
    /// Выдает ссылку на основное изображение пользователя
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<BaseResponse<string, DataImage>> GetPrimaryImage(string accessToken, string? personId = null);

    /// <summary>
    /// Устанавливает изображение основным
    /// </summary>
    /// <param name="accessToken">Access токена</param>
    /// <param name="imageId">Идентификатор изображения</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> SetImageIsPrimary(string accessToken, string imageId);

    /// <summary>
    /// Удаляет изображение
    /// </summary>
    /// <param name="accessToken">Access токена</param>
    /// <param name="imageId">Идентификатор изображения</param>
    /// <returns></returns>
    public Task<BaseResponse<string, string>> DeleteImage(string accessToken, string imageId);
}