using AuthService.Enums;
using AuthService.Models.DB;
using AuthService.Models.Response;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Service;

public class SessionService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public SessionService(IAuthRepository authRepository, IEncryptionService encryptionService, JwtTokenService jwtTokenService, ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }
    
    /// <summary>
    /// Выдает все активные сессии
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, DataSession>> GetSessions(string accessToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, DataSession> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        IQueryable<Session> sessions = _authRepository.GetSessionsAsync(dataToken.PersonId);
        
        if (!sessions.Any())
            return new BaseResponse<string, DataSession> { Message = "Сессии не найдены", Successfully = false, Status = 404, Type = ResponseType.SessionNotFound, Errors = "Not Found", Data = null };
            
        List<Sessions> result = new ();
        foreach (var session in sessions)
        {
            var address = await DeterminingIpAddress.GetPositionUser(_encryptionService.Decrypt(session.IpAddress));
            Sessions data = new Sessions
            {
                IpAddress = _encryptionService.Decrypt(session.IpAddress),
                City = address.City,
                Country = address.Country,
                Device = session.Device,
                Os = session.Os,
                Browser = session.Browser,
                CreateAt = session.CreatedAt,
                LastUsedAt = session.LastUsedAt,
                SessionId = session.SessionId,
                IsYou = dataToken.Id == session.Id
            };
            result.Add(data);
        }
        
        return new BaseResponse<string, DataSession> { Message = "Ваши активные сессии", Successfully = true, Status = 200, Type = ResponseType.Ok, Errors = null, Data = new DataSession { Count = result.Count, Sessions = result } };
    }
    
    /// <summary>
    /// Отзывает сессии
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, List<string>>> RevokeSession(string accessToken, string? sessionId = null)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);

        if (await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            IQueryable<Session> sessions = _authRepository.GetSessionsAsync(dataToken.PersonId);

            if (!sessions.Any())
                return new BaseResponse<string, List<string>> { Message = "Активные сессии не найдены", Successfully = false, Status = 404, Type = ResponseType.SessionNotFound, Errors = "Not Found", Data = null };
            
            if (sessionId != null)
            {
                var session = sessions.FirstOrDefault(s => s.SessionId == sessionId && s.IsRevoked == false);
                if (session == null)
                    return new BaseResponse<string, List<string>> { Message = "Данная сессия не найдена", Successfully = false, Status = 404, Type = ResponseType.SessionNotFound, Errors = "Not Found", Data = null };
                    
                session.IsRevoked = true;
                await _authRepository.AddSessionToBanAsync(sessionId, dataToken.PersonId);
                await _authRepository.SaveChangesAsync();

                return new BaseResponse<string, List<string>> { Message = "Сессия успешно отозвана", Successfully = true, Status = 200, Type = ResponseType.Ok, Errors = null, Data = new List<string> { sessionId } };
            }

            IQueryable<Session> sessionsRevoke = sessions.Where(s => s.PersonId == dataToken.PersonId && s.Id != dataToken.Id);
            var sessionIdsToRevoke = sessionsRevoke.Select(s => s.SessionId).ToList();
            
            await _authRepository.AddSessionsToBanAsync(sessionsRevoke.Select(s => s.SessionId), dataToken.PersonId);
            await sessionsRevoke.ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRevoked, s => true));
            
            return new BaseResponse<string, List<string>> { Message = "Сессии успешно отозваны", Successfully = true, Status = 200, Type = ResponseType.Ok, Errors = null, Data = sessionIdsToRevoke };
        }
        return new BaseResponse<string, List<string>> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
    }
    
}