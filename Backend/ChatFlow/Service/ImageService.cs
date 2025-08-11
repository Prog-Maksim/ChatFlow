using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts.Interfaces;
using ChatFlow.Service.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ChatFlow.Service;

public class ImageService: IImageService
{
    private const long MaxFileSize = 4_194_304; // 4 МБ
    private const int MinImageSize = 500;
    private const int MaxImagesPerUser = 25;
    
    private readonly ILogger<ImageService> _logger;
    private readonly IS3Service _s3Service;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IProfileRepository _profileRepository;
    
    public ImageService(IS3Service s3Service, IJwtTokenService jwtTokenService, IProfileRepository profileRepository, ILogger<ImageService> logger)
    {
        _logger = logger;
        _s3Service = s3Service;
        _jwtTokenService = jwtTokenService;
        _profileRepository = profileRepository;
    }
    
    public async Task<BaseResponse<string, string>> UploadFile(IFormFile file, string accessToken, double? top = 0, double? left = 0)
    {
        if (file.Length == 0)
            return CreateErrorResponse<string, string>("Файл не выбран или пуст", ResponseType.InvalidFile, 400, "FileMissing");

        var tokenData = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(tokenData))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        if (!await _profileRepository.UserExistsAsync(tokenData.PersonId))
        {
            _logger.LogWarning("Пользователь не найден: {personId}", tokenData.PersonId);
            return CreateErrorResponse<string, string>("Пользователь не найден!", ResponseType.PersonNotFound, 404, "Not Found");
        }

        if (await _profileRepository.GetNumImageInByIdAsync(tokenData.PersonId) >= MaxImagesPerUser)
            return CreateErrorResponse<string, string>("Превышен лимит изображений (25)", ResponseType.ImageLimitReached, 409, "ImageLimit");

        if (file.Length > MaxFileSize)
            return CreateErrorResponse<string, string>("Файл слишком большой", ResponseType.FileTooLarge, 413, "FileTooLarge");

        try
        {
            Image? image = await CropImageAsync(file, MinImageSize, MinImageSize, (int)(left ?? 0), (int)(top ?? 0));

            if (image == null)
                return CreateErrorResponse<string, string>($"Изображение должно быть не менее {MinImageSize}x{MinImageSize}px", ResponseType.InvalidFile, 400, "ImageTooSmall");

            await using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms);
            ms.Position = 0;

            var processedFile = new FormFile(ms, 0, ms.Length, file.Name, file.FileName)
            {
                Headers = file.Headers,
                ContentType = file.ContentType
            };

            var fileKey = await _s3Service.UploadFileAsync(processedFile);

            var imageRecord = new Images
            {
                PersonId = tokenData.PersonId,
                ImageId = fileKey,
                IsPrimary = true
            };

            await _profileRepository.ClearPrimaryImageAsync(tokenData.PersonId);
            await _profileRepository.AddImageDataAsync(imageRecord);
            await _profileRepository.SaveChangesAsync();

            return new BaseResponse<string, string>
            {
                Message = "Файл успешно загружен",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Data = S3Service.BaseFileUrl + fileKey
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке изображения");
            return CreateErrorResponse<string, string>("Ошибка при обработке изображения", ResponseType.ErrorUploadFile, 500, "ErrorUploadFile");
        }
    }
    
    public async Task<BaseResponse<string, CountImage>> GetCountImages(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, CountImage>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        if (personId is null)
        {
            var count = await _profileRepository.GetNumImageInByIdAsync(dataToken.PersonId);
            return new BaseResponse<string, CountImage>
            {
                Message = "Кол-во изображений",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = new CountImage { Count = count},
            };
        }
        
        if (!await _profileRepository.UserExistsAsync(personId))
        {
            _logger.LogError("Пользователь под id: {personId} не найден!", personId);
            return CreateErrorResponse<string, CountImage>("Пользователь не найден!", ResponseType.PersonNotFound, 404, "Not Found");
        }
        
        return new BaseResponse<string, CountImage>
        {
            Message = "Кол-во изображений",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Errors = null,
            Data = new CountImage { Count = await _profileRepository.GetNumImageInByIdAsync(personId) },
        };
    }
    
    public async Task<BaseResponse<string, DataImage>> GetPrimaryImage(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, DataImage>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        if (personId is null)
        {
            var imageId = await _profileRepository.GetPrimaryImage(dataToken.PersonId);

            if (imageId is null)
                return CreateErrorResponse<string, DataImage>("Ссылка на главное изображение не найдено", ResponseType.ImageNotFound, 404, "Not Found");
            
            return new BaseResponse<string, DataImage>
            {
                Message = "Основное изображение пользователя",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = new DataImage
                {
                    ImageId = imageId,
                    Url = S3Service.BaseFileUrl + imageId
                }
            };
        }
        
        if (!await _profileRepository.UserExistsAsync(personId))
        {
            _logger.LogError("Пользователь под id: {personId} не найден!", personId);
            return CreateErrorResponse<string, DataImage>("Пользователь не найден!", ResponseType.PersonNotFound, 404, "Not Found");
        }
        
        var imageId1 = await _profileRepository.GetPrimaryImage(personId);
        
        if (imageId1 is null)
            return CreateErrorResponse<string, DataImage>("Ссылка на главное изображение не найдено", ResponseType.ImageNotFound, 404, "Not Found");
        
        return new BaseResponse<string, DataImage>
        {
            Message = "Основное изображение пользователя",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Errors = null,
            Data = new DataImage
            {
                ImageId = imageId1,
                Url = S3Service.BaseFileUrl + imageId1
            }
        };
    }
    
    public async Task<BaseResponse<string, string>> SetImageIsPrimary(string accessToken, string imageId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        await _profileRepository.ClearPrimaryImageAsync(dataToken.PersonId);

        try
        {
            await _profileRepository.SetPrimaryImageAsync(dataToken.PersonId, imageId);

            return new BaseResponse<string, string>
            {
                Message = "Изображение сделано основным",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = S3Service.BaseFileUrl + imageId
            };
        }
        catch (FileNotFoundException)
        {
            return CreateErrorResponse<string, string>("Изображение не найдено", ResponseType.ImageNotFound, 404, "Not Found");
        }
    }

    public async Task<BaseResponse<string, string>> DeleteImage(string accessToken, string imageId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        var image = await _profileRepository.GetImageByIdAsync(dataToken.PersonId, imageId);
        
        if (image is null)
            return CreateErrorResponse<string, string>("Изображение не найдено", ResponseType.ImageNotFound, 404, "Not Found");
        
        bool wasPrimary = image.IsPrimary;

        _profileRepository.DeleteImage(image);
        await _profileRepository.SaveChangesAsync();

        if (wasPrimary)
            await _profileRepository.SetFirstImageIsPrimaryAsync(dataToken.PersonId);
        
        return new BaseResponse<string, string>
        {
            Message = "Изображение успешно удалено",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Errors = null,
            Data = S3Service.BaseFileUrl + imageId
        };
    }
    
    private static async Task<Image?> CropImageAsync(IFormFile file, int cropWidth, int cropHeight, int x, int y)
    {
        var image = await Image.LoadAsync(file.OpenReadStream());

        if (image.Width < cropWidth || image.Height < cropHeight)
            return null;

        x = Math.Clamp(x, 0, image.Width - cropWidth);
        y = Math.Clamp(y, 0, image.Height - cropHeight);
        int finalCropWidth = Math.Min(cropWidth, image.Width - x);
        int finalCropHeight = Math.Min(cropHeight, image.Height - y);

        image.Mutate(i => i.Crop(new Rectangle(x, y, finalCropWidth, finalCropHeight)));

        return image;
    }
    
    private BaseResponse<TErrors, TData> CreateErrorResponse<TErrors, TData>(string message, ResponseType type, int status, TErrors errors)
    {
        return new BaseResponse<TErrors, TData>
        {
            Message = message,
            Type = type,
            Status = status,
            Successfully = false,
            Data = default,
            Errors = errors
        };
    }
}