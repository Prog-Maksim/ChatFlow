using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service.Interfaces;
using Sessions = ChatFlow.Models.DB.Sessions;

namespace ChatFlow.Service;

public class TokenService: ITokenService
{
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    public TokenService(IAuthRepository authRepository, IJwtTokenService jwtTokenService)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
    }
    
    public async Task<BaseResponse<string, AuthTokens>> RefreshAccessToken(string refreshToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(refreshToken);
        Persons? person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
        
        if (person is null || person.AccountState == AccountState.Blocked)
            return ResponseFactory.PersonNotFoundOrBlocked<AuthTokens>();
        
        if (!await _jwtTokenService.ValidateJwtRefreshToken(dataToken, person))
            return ResponseFactory.JwtTokenInvalid<AuthTokens>();
        
        var session = await _authRepository.GetSessionByIdAsync(person.PersonId, dataToken.SessionId);

        if (session == null || session.IsRevoked)
            return ResponseFactory.AccessDenied<AuthTokens>();
            
        session.LastUsedAt = DateTime.UtcNow;
        await _authRepository.SaveChangesAsync();
            
        var authTokens = GenerateAuthTokens(person, session, refreshToken);
        return ResponseFactory.Success("Токены успешно обновлены", authTokens);
    }
    
    /// <summary>
    /// Генерация новых токенов
    /// </summary>
    /// <param name="person">Идентификатор пользователя</param>
    /// <param name="session">Объект сессии</param>
    /// <param name="previousRefreshToken">Текущий refresh токен</param>
    /// <returns></returns>
    private AuthTokens GenerateAuthTokens(Persons person, Sessions session, string previousRefreshToken)
    {
        var tokens = _jwtTokenService.CreateJwtToken(
            person.PersonId,
            person.PasswordVersion,
            session.SessionId,
            session.DeviceId,
            session.Id,
            previousRefreshToken
        );
    
        return new AuthTokens
        {
            PersonId = person.PersonId,
            DeviceId = session.DeviceId,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute)
        };
    }
}