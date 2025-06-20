using AuthService.Enums;
using AuthService.Models.Response;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;

namespace AuthService.Service;

public class TokenService
{
    private readonly IAuthRepository _authRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public TokenService(IAuthRepository authRepository, JwtTokenService jwtTokenService, ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }
    
    /// <summary>
    /// Обновляет Refresh токен
    /// </summary>
    /// <param name="refreshToken">Refresh токен</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, AuthTokens>> RefreshAccessToken(string refreshToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(refreshToken);
        var person = await _authRepository.GetUserByIdAsync(dataToken.PersonId);
        
        if (person == null || person.AccountState == AccountState.Blocked)
            return new BaseResponse<string, AuthTokens> { Message = "Пользователь не найден или был заблокирован!", Type = ResponseType.PersonNotFoundOrBlocked, Errors = "Forbidden", Status = 423, Successfully = false, Data = null};

        if (await _jwtTokenService.ValidateJwtRefreshToken(dataToken, person))
        {
            var session = await _authRepository.GetSessionByIdAsync(person.PersonId, dataToken.SessionId);

            if (session == null || session.IsRevoked)
                return new BaseResponse<string, AuthTokens> { Message = "Отказано", Successfully = false, Status = 403, Type = ResponseType.AccessDenied, Errors = "Forbidden", Data = null };
            
            session.LastUsedAt = DateTime.UtcNow;
            await _authRepository.SaveChangesAsync();
            
            var tokens = _jwtTokenService.CreateJwtToken(person.PersonId, person.PasswordVersion, session.SessionId, session.Id, refreshToken);
            
            var tokensResult = new AuthTokens { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken, AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute), RefreshTokenExpiration = DateTime.UtcNow.AddDays(JwtTokenService.RefreshTokenLifetimeDay) };
            return new BaseResponse<string, AuthTokens> { Message = "Токены успешно обновлены", Successfully = true, Status = 200, Type = ResponseType.Ok, Errors = null, Data = tokensResult, };
        }
        return new BaseResponse<string, AuthTokens> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
    }
}