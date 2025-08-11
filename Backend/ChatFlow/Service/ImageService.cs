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
        const int minImageSize = 500;
        const int maxImagesPerUser = 25;

        if (file.Length == 0)
            return ErrorResponse("Файл не выбран или пуст", 400, ResponseType.InvalidFile, "FileMissing");

        var tokenData = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(tokenData))
            return ErrorResponse("Неверный JWT токен", 403, ResponseType.JwtTokenVerificationFailed, "Forbidden");

        if (!await _profileRepository.UserExistsAsync(tokenData.PersonId))
        {
            _logger.LogWarning("Пользователь не найден: {personId}", tokenData.PersonId);
            return ErrorResponse("Пользователь не найден", 404, ResponseType.PersonNotFound, "UserNotFound");
        }

        if (await _profileRepository.GetNumImageInByIdAsync(tokenData.PersonId) >= maxImagesPerUser)
            return ErrorResponse("Превышен лимит изображений (25)", 409, ResponseType.ImageLimitReached, "ImageLimit");

        if (file.Length > MaxFileSize)
            return ErrorResponse("Файл слишком большой", 413, ResponseType.FileTooLarge, "FileTooLarge");

        try
        {
            Image? image = await CropImageAsync(file, minImageSize, minImageSize, (int)(left ?? 0), (int)(top ?? 0));

            if (image == null)
                return ErrorResponse($"Изображение должно быть не менее {minImageSize}x{minImageSize}px", 400, ResponseType.InvalidFile, "ImageTooSmall");

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
            return ErrorResponse("Ошибка при обработке изображения", 500, ResponseType.ErrorUploadFile, "ErrorUploadFile");
        }
    }
    
    public async Task<BaseResponse<string, CountImage>> GetCountImages(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, CountImage> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null };

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
            return new BaseResponse<string, CountImage> { Message = "Пользователь не найден!", Successfully = false, Status = 404, Type = ResponseType.ImageLimitReached, Errors = "Not Found", Data = null };
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
            return new BaseResponse<string, DataImage> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (personId is null)
        {
            var imageId = await _profileRepository.GetPrimaryImage(dataToken.PersonId);

            if (imageId is null)
                return new BaseResponse<string, DataImage>
                {
                    Message = "Ссылка на главное изображение не найдено",
                    Successfully = false,
                    Status = 404,
                    Type = ResponseType.ImageNotFound,
                    Errors = "Not Found",
                    Data = null
                };
            
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
            return new BaseResponse<string, DataImage> { Message = "Пользователь не найден!", Successfully = false, Status = 404, Type = ResponseType.ImageLimitReached, Errors = "Not Found", Data = null };
        }
        
        var imageId1 = await _profileRepository.GetPrimaryImage(personId);
        
        if (imageId1 is null)
            return new BaseResponse<string, DataImage>
            {
                Message = "Ссылка на главное изображение не найдено",
                Successfully = false,
                Status = 404,
                Type = ResponseType.ImageNotFound,
                Errors = "Not Found",
                Data = null
            };
        
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
            return new BaseResponse<string, string> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

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
            return new BaseResponse<string, string>
            {
                Message = "Изображение не найдено",
                Successfully = false,
                Status = 404,
                Type = ResponseType.ImageNotFound,
                Errors = "Not Found",
                Data = null
            };
        }
    }

    public async Task<BaseResponse<string, string>> DeleteImage(string accessToken, string imageId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, string> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        var image = await _profileRepository.GetImageByIdAsync(dataToken.PersonId, imageId);
        
        if (image is null)
            return new BaseResponse<string, string>
            {
                Message = "Изображение не найдено",
                Successfully = false,
                Status = 404,
                Type = ResponseType.ImageNotFound,
                Errors = "Not Found",
                Data = null
            };
        
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
    
    private BaseResponse<string, string> ErrorResponse(string message, int status, ResponseType type, string error)
    {
        return new BaseResponse<string, string>
        {
            Message = message,
            Successfully = false,
            Status = status,
            Type = type,
            Errors = error,
            Data = null
        };
    }
}