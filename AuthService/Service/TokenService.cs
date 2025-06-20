using AuthService.Enums;
using AuthService.Models.DB;
using AuthService.Models.Response;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;

namespace AuthService.Service;

public class TokenService
{
    private readonly IAuthRepository _authRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly TokenValidator _tokenValidator;

    public TokenService(IAuthRepository authRepository, JwtTokenService jwtTokenService, TokenValidator tokenValidator)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _tokenValidator = tokenValidator;
    }
    
    /// <summary>
    /// Обновляет Refresh токен
    /// </summary>
    /// <param name="refreshToken">Refresh токен</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, AuthTokens>> RefreshAccessToken(string refreshToken)
    {
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(refreshToken);
        var person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
        
        if (person == null || person.AccountState == AccountState.Blocked)
            return ResponseFactory.PersonNotFoundOrBlocked<AuthTokens>();
        
        if (!isValid)
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
    private AuthTokens GenerateAuthTokens(Person person, Session session, string previousRefreshToken)
    {
        var tokens = _jwtTokenService.CreateJwtToken(
            person.PersonId,
            person.PasswordVersion,
            session.SessionId,
            session.Id,
            previousRefreshToken
        );

        return new AuthTokens
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute),
            RefreshTokenExpiration = DateTime.UtcNow.AddDays(JwtTokenService.RefreshTokenLifetimeDay)
        };
    }
}