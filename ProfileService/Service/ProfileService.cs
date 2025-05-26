using ProfileService.Enums;
using ProfileService.Models.Other;
using ProfileService.Models.Requests;
using ProfileService.Models.Response;
using ProfileService.Repository.Interfaces;
using ProfileService.Scripts;

namespace ProfileService.Service;

public class ProfileService
{
    private readonly ILogger<ProfileService> _logger;
    private readonly IProfileRepository _profileRepository;
    private readonly JwtTokenService _jwtTokenService;
    
    public ProfileService(ILogger<ProfileService> logger, IProfileRepository profileRepository, JwtTokenService jwtTokenService)
    {
        _logger = logger;
        _profileRepository = profileRepository;
        _jwtTokenService = jwtTokenService;
    }
    
    /// <summary>
    /// Выдает краткую информацию о профиле
    /// </summary>
    /// <param name="accessToken">Идентификатор токена</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, SummaryDataPerson>> GetSummaryProfileData(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, SummaryDataPerson> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(dataToken.PersonId))
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
    
    /// <summary>
    /// Выдает полную информацию о профиле
    /// </summary>
    /// <param name="accessToken">Идентификатор токена</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, DataPerson>> GetProfileData(string accessToken, string? personId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, DataPerson> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (personId is not null)
        {
            if (!await _profileRepository.UserExistsAsync(dataToken.PersonId))
            {
                _logger.LogError("Пользователь под id: {personId} не найден!", dataToken.PersonId);
                return new BaseResponse<string, DataPerson> { Message = "Пользователь не найден!", Successfully = false, Status = 404, Type = ResponseType.ImageLimitReached, Errors = "Not Found", Data = null };
            }

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

    /// <summary>
    /// Обновляет информацию в профиле
    /// </summary>
    /// <param name="accessToken">Идентификатор токена</param>
    /// <param name="profile">Данные профиля</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, string>> UpdateProfileData(string accessToken, Profile profile)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, string> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (profile.Tag is not null && !await _profileRepository.CheckTagAsync(profile.Tag))
            return new BaseResponse<string, string> { Message = "Данный тег занят", Type = ResponseType.TagAlreadyExists, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        var result = await _profileRepository.UpdateProfileDataAsync(dataToken.PersonId, profile);
        
        if (result)
            return new BaseResponse<string, string> { Message = "Данные были успешно обновлены", Type = ResponseType.Ok, Status = 200, Successfully = true, Data = null};

        return new BaseResponse<string, string> { Message = "Не найдены данные для обновления", Type = ResponseType.UpdateDataNotFound, Errors = "NotFound", Status = 404, Successfully = false, Data = null};
    }
}