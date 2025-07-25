using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ChatFlow.Service;

public class ImageService: IImageService
{
    private const long MaxFileSize = 4_194_304; // 4 МБ
    private readonly ILogger<ImageService> _logger;
    private readonly S3Service _s3Service;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IProfileRepository _profileRepository;
    
    public ImageService(S3Service s3Service, JwtTokenService jwtTokenService, IProfileRepository profileRepository, ILogger<ImageService> logger)
    {
        _logger = logger;
        _s3Service = s3Service;
        _jwtTokenService = jwtTokenService;
        _profileRepository = profileRepository;
    }
    
    public async Task<BaseResponse<string, string>> UploadFile(IFormFile file, string accessToken, double? top = 0, double? left = 0)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, string> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (!await _profileRepository.UserExistsAsync(dataToken.PersonId))
        {
            _logger.LogError("Пользователь под id: {personId} не найден!", dataToken.PersonId);
            return new BaseResponse<string, string> { Message = "Пользователь не найден!", Successfully = false, Status = 404, Type = ResponseType.ImageLimitReached, Errors = "Not Found", Data = null };
        }
        
        if (await _profileRepository.GetNumImageInByIdAsync(dataToken.PersonId) >= 25)
            return new BaseResponse<string, string> { Message = "Допущено предельное кол-во изображений в 25 штук", Successfully = false, Status = 409, Type = ResponseType.ImageLimitReached, Errors = "Conflict", Data = null };
        
        if (file.Length > MaxFileSize)
            return new BaseResponse<string, string> { Message = "Файл слишком большой", Successfully = false, Status = 403, Type = ResponseType.FileTooLarge, Errors = "Forbidden", Data = null };

        try
        {
            using var image = await Image.LoadAsync(file.OpenReadStream());

            if (image.Width < 500 || image.Height < 500)
                return new BaseResponse<string, string> { Message = "Изображение должно быть не меньше 500x500 пикселей", Successfully = false, Status = 400, Type = ResponseType.InvalidFile, Errors = "ImageTooSmall", Data = null };
            
            int cropWidth = 500;
            int cropHeight = 500;
            int x = (int)(left ?? 0);
            int y = (int)(top ?? 0);
            
            if (x + cropWidth > image.Width) cropWidth = image.Width - x;
            if (y + cropHeight > image.Height) cropHeight = image.Height - y;
            
            image.Mutate(i => i.Crop(new Rectangle(x, y, cropWidth, cropHeight)));
            
            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms);
            ms.Position = 0;
            
            var formFile = new FormFile(ms, 0, ms.Length, file.Name, file.FileName) { Headers = file.Headers, ContentType = file.ContentType };
            var fileKey = await _s3Service.UploadFileToS3Async(formFile);

            Images imageFile = new Images
            {
                PersonId = dataToken.PersonId,
                ImageId = fileKey,
                IsPrimary = true
            };

            await _profileRepository.ClearPrimaryImageAsync(dataToken.PersonId);
            await _profileRepository.AddImageDataAsync(imageFile);
            await _profileRepository.SaveChangesAsync();

            return new BaseResponse<string, string> { Message = "Файл успешно загружен", Successfully = true, Status = 200, Type = ResponseType.Ok, Data = S3Service.BaseFileUrl + imageFile.ImageId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке изображения");
            return new BaseResponse<string, string> { Message = "Ошибка при обработке изображения", Successfully = false, Status = 500, Type = ResponseType.ErrorUploadFile, Errors = ex.Message, Data = null };
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
}