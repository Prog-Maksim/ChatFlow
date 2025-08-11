using System.Text.Json;
using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts.Interfaces;
using ChatFlow.Service.Interfaces;
using StackExchange.Redis;

namespace ChatFlow.Service;

public class ProfileService: IProfileService
{
    private readonly ILogger<ProfileService> _logger;
    private readonly IProfileRepository _profileRepository;
    private readonly ISearchRepository _searchRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtherPersonDataRepository _otherPersonDataRepository;
    private readonly IDatabase _redis;
    
    public ProfileService(ILogger<ProfileService> logger, IProfileRepository profileRepository, IJwtTokenService jwtTokenService, ISearchRepository searchRepository, IOtherPersonDataRepository otherPersonDataRepository, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _profileRepository = profileRepository;
        _jwtTokenService = jwtTokenService;
        _searchRepository = searchRepository;
        _otherPersonDataRepository = otherPersonDataRepository;
        _redis = redis.GetDatabase();
    }
    
    public async Task<BaseResponse<string, SummaryDataPerson>> GetSummaryProfileData(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, SummaryDataPerson>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(personId))
            {
                _logger.LogError("Пользователь под id: {personId} не найден!", dataToken.PersonId);
                return CreateErrorResponse<string, SummaryDataPerson>("Пользователь не найден!", ResponseType.PersonNotFound, 404, "Not Found");
            }

            var data = await _profileRepository.GetSummaryPersonDataAsync(personId);
            
            return new BaseResponse<string, SummaryDataPerson>
            {
                Message = "Краткие данные пользователя",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = data
            };
        }
        
        var data1 = await _profileRepository.GetSummaryPersonDataAsync(dataToken.PersonId);
        return new BaseResponse<string, SummaryDataPerson>
        {
            Message = "Краткие данные пользователя",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Errors = null,
            Data = data1
        };
    }
    
    public async Task<BaseResponse<string, DataPerson>> GetProfileData(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, DataPerson>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(personId))
                return CreateErrorResponse<string, DataPerson>("Пользователь не найден!", ResponseType.PersonNotFound, 404, "Not Found");
        
            var data = await _profileRepository.GetPersonDataAsync(personId);
            return new BaseResponse<string, DataPerson>
            {
                Message = "Данные пользователя",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = data
            };
        }
        
        var data1 = await _profileRepository.GetPersonDataAsync(dataToken.PersonId);
        return new BaseResponse<string, DataPerson>
        {
            Message = "Данные пользователя",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Errors = null,
            Data = data1
        };
    }
    
    public async Task<BaseResponse<string, string>> UpdateProfileData(string accessToken, Profile profile)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        DataPersons? personTag = null;
        if (profile.Tag is not null)
            personTag = await _profileRepository.CheckTagAsync(profile.Tag);
        
        if (profile.Tag is not null && personTag != null && personTag.PersonId != dataToken.PersonId)
            return CreateErrorResponse<string, string>("Данный тег занят!", ResponseType.TagAlreadyExists, 409, "Conflict");
        
        await _profileRepository.UpdateProfileDataAsync(dataToken.PersonId, profile);
        await _searchRepository.UpdatePersonAsync(new UserCreated
        {
            PersonId = dataToken.PersonId,
            Name = profile.Name,
            Surname = profile.Surname,
            Tag = "@" + profile.Tag
        });
        
            
        return new BaseResponse<string, string>
        {
            Message = "Данные были успешно обновлены", Type = ResponseType.Ok, Status = 200, Successfully = true,
            Data = null
        };
    }
    
    public async Task<BaseResponse<string, List<DataImage>>> GetProfileImages(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, List<DataImage>>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(personId))
                return CreateErrorResponse<string, List<DataImage>>("Пользователь не найден!", ResponseType.PersonNotFound, 404, "Not Found");
            
            var data = await _profileRepository.GetImagesPersonData(personId);
            return new BaseResponse<string, List<DataImage>>
            {
                Message = "Изображения пользователя",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = data
            };
        }
        
        var data1 = await _profileRepository.GetImagesPersonData(dataToken.PersonId);
        return new BaseResponse<string, List<DataImage>>
        {
            Message = "Изображения пользователя",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Errors = null,
            Data = data1
        };
    }

    public async Task<BaseResponse<string, List<PublicKeyResponse>>> GetPublicKeyAsync(string personId)
    {
        string cacheKey = $"public_keys:{personId}";
        
        var cachedData = await _redis.StringGetAsync(cacheKey);
        if (cachedData.HasValue)
        {
            var keys = JsonSerializer.Deserialize<List<PublicKeyResponse>>(cachedData.ToString())!;
            return new BaseResponse<string, List<PublicKeyResponse>>
            {
                Message = "Ключи пользователя",
                Successfully = true,
                Status = 200,
                Type = ResponseType.Ok,
                Errors = null,
                Data = keys
            };
        }
        
        List<PublicKeyResponse> keysFromDb = await _otherPersonDataRepository.GetActivePublicKeys(personId);
        
        if (keysFromDb.Count == 0)
            return CreateErrorResponse<string, List<PublicKeyResponse>>("Ключи не найдены", ResponseType.KeysNotFound, 404, "Not Found");

        var isSet = await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(keysFromDb), TimeSpan.FromDays(1));
        if (!isSet)
            _logger.LogWarning("Не удалось сохранить ключи пользователя {PersonId} в Redis", personId);
        
        return new BaseResponse<string, List<PublicKeyResponse>>
        {
            Message = "Ключи пользователя",
            Successfully = true,
            Status = 200,
            Type = ResponseType.Ok,
            Errors = null,
            Data = keysFromDb
        };
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