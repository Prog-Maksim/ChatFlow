using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service.Interfaces;

namespace ChatFlow.Service;

public class ProfileService: IProfileService
{
    private readonly ILogger<ProfileService> _logger;
    private readonly IProfileRepository _profileRepository;
    private readonly ISearchRepository _searchRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtherPersonDataRepository _otherPersonDataRepository;
    
    public ProfileService(ILogger<ProfileService> logger, IProfileRepository profileRepository, IJwtTokenService jwtTokenService, ISearchRepository searchRepository, IOtherPersonDataRepository otherPersonDataRepository)
    {
        _logger = logger;
        _profileRepository = profileRepository;
        _jwtTokenService = jwtTokenService;
        _searchRepository = searchRepository;
        _otherPersonDataRepository = otherPersonDataRepository;
    }
    
    public async Task<BaseResponse<string, SummaryDataPerson>> GetSummaryProfileData(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, SummaryDataPerson> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(personId))
            {
                _logger.LogError("Пользователь под id: {personId} не найден!", dataToken.PersonId);
                return new BaseResponse<string, SummaryDataPerson> { Message = "Пользователь не найден!", Successfully = false, Status = 404, Type = ResponseType.ImageLimitReached, Errors = "Not Found", Data = null };
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
            return new BaseResponse<string, DataPerson> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
        
        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(personId))
                return new BaseResponse<string, DataPerson> { Message = "Пользователь не найден!", Successfully = false, Status = 404, Type = ResponseType.ImageLimitReached, Errors = "Not Found", Data = null };
        
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
            return new BaseResponse<string, string> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
        
        DataPersons? personTag = null;
        if (profile.Tag is not null)
            personTag = await _profileRepository.CheckTagAsync(profile.Tag);
        
        if (profile.Tag is not null && (personTag != null && personTag.PersonId != dataToken.PersonId))
            return new BaseResponse<string, string> { Message = "Данный тег занят", Type = ResponseType.TagAlreadyExists, Errors = "Conflict", Status = 409, Successfully = false, Data = null};
        
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
    
    public async Task<BaseResponse<string, List<DataImage>>> GetProfileImages(string accessToken,
        string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, List<DataImage>> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(personId))
                return new BaseResponse<string, List<DataImage>> { Message = "Пользователь не найден!", Successfully = false, Status = 404, Type = ResponseType.ImageLimitReached, Errors = "Not Found", Data = null };
            
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
        List<PublicKeyResponse> keys = await _otherPersonDataRepository.GetActivePublicKeys(personId);
        
        if (keys.Count == 0)
            return new BaseResponse<string, List<PublicKeyResponse>> { Message = "Ключи не найдены", Successfully = false, Status = 404, Type = ResponseType.KeysNotFound, Errors = "Not Found", Data = null };

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
}