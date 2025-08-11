using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Scripts.Interfaces;
using ChatFlow.Service.Interfaces;

namespace ChatFlow.Service;

public class SessionService: ISessionService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<SessionService> _logger;
    private readonly IWebSocketConnectionManager _manager;

    public SessionService(IAuthRepository authRepository, IEncryptionService encryptionService, ITokenValidator tokenValidator, ILogger<SessionService> logger, IWebSocketConnectionManager manager)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _tokenValidator = tokenValidator;
        _logger = logger;
        _manager = manager;
    }
    
    public async Task<BaseResponse<string, DataSession>> GetSessions(string accessToken)
    {
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(accessToken);
        if (!isValid)
            return ResponseFactory.JwtTokenInvalid<DataSession>();
        
        var sessions = _authRepository.GetSessionsAsync(dataToken!.PersonId).ToList();
        if (sessions.Count == 0)
            return ResponseFactory.SessionNotFound<DataSession>();
        
        var data = await BuildSessionDataAsync(sessions, dataToken.Id);
        return ResponseFactory.Success("Ваши активные сессии", data);
    }
    
    public async Task<BaseResponse<string, List<RevokeSession>>> RevokeSession(string accessToken, string? sessionId = null)
    {
        var (isValid, dataToken) = await _tokenValidator.TryValidateTokenAsync(accessToken);
        if (!isValid)
            return ResponseFactory.JwtTokenInvalid<List<RevokeSession>>();

        var sessions = _authRepository.GetSessionsAsync(dataToken!.PersonId).ToList();
        if (sessions.Count == 0)
            return ResponseFactory.SessionNotFound<List<RevokeSession>>();

        return sessionId != null
            ? await RevokeSingleSessionAsync(sessionId, sessions, dataToken.PersonId)
            : await RevokeAllOtherSessionsAsync(sessions, dataToken.PersonId, dataToken.Id);
    }
    
    public async Task CloseConnection(string sessionId, string personId)
    {
        await _manager.RemoveConnection(personId, sessionId);
    }
    
    public async Task CloseConnection(List<string> sessionId, string personId)
    {
        foreach (var session in sessionId)
            await _manager.RemoveConnection(personId, session);
    }
    
    /// <summary>
    /// Сборка активных сессий
    /// </summary>
    /// <param name="sessions">Список сессий</param>
    /// <param name="currentSessionId">Текущий идентификатор сессий</param>
    /// <returns></returns>
    private async Task<DataSession> BuildSessionDataAsync(List<Models.DB.Sessions> sessions, int currentSessionId)
    {
        var sessionResults = await Task.WhenAll(sessions.Select(async session =>
        {
            var decryptedIp = _encryptionService.Decrypt(session.IpAddress);
            var location = await DeterminingIpAddress.GetPositionUser(decryptedIp, _logger);

            return new Sessions
            {
                IpAddress = decryptedIp,
                City = location.City,
                Country = location.Country,
                Device = session.Device,
                Os = session.Os,
                Browser = session.Browser,
                CreateAt = session.CreatedAt,
                LastUsedAt = session.LastUsedAt,
                SessionId = session.SessionId,
                IsYou = currentSessionId == session.Id
            };
        }));

        return new DataSession
        {
            Count = sessionResults.Length,
            Sessions = sessionResults.ToList()
        };
    }
    
    /// <summary>
    /// Отзыв одной сессии
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="sessions">Список сессий</param>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    private async Task<BaseResponse<string, List<RevokeSession>>> RevokeSingleSessionAsync(string sessionId, List<Models.DB.Sessions> sessions, string personId)
    {
        var session = sessions.FirstOrDefault(s => s.SessionId == sessionId && !s.IsRevoked);
        if (session == null)
            return ResponseFactory.SessionNotFound<List<RevokeSession>>();

        session.IsRevoked = true;
        await _authRepository.AddSessionToBanAsync(sessionId, personId);
        await _authRepository.SaveChangesAsync();
        _ = CloseConnection(sessionId, personId);

        return ResponseFactory.Success("Сессия успешно отозвана", new List<RevokeSession> { new RevokeSession { SessionId = sessionId } });
    }

    /// <summary>
    /// Отзыв всех сессии
    /// </summary>
    /// <param name="sessions">Список сессий</param>
    /// <param name="personId">Идентификатор сессии</param>
    /// <param name="currentSessionId">Текущий идентификатор сессии</param>
    /// <returns></returns>
    private async Task<BaseResponse<string, List<RevokeSession>>> RevokeAllOtherSessionsAsync(List<Models.DB.Sessions> sessions, string personId, int currentSessionId)
    {
        var sessionsToRevoke = sessions.Where(s => s.Id != currentSessionId && !s.IsRevoked).ToList();
        if (sessionsToRevoke.Count == 0)
            return ResponseFactory.SessionNotFound<List<RevokeSession>>();

        var sessionIds = sessionsToRevoke.Select(s => s.SessionId).ToList();
        _ = CloseConnection(sessionIds, personId);
        
        await _authRepository.AddSessionsToBanAsync(sessionIds, personId);
        
        return ResponseFactory.Success("Сессии успешно отозваны", sessionIds.Select(s => new RevokeSession { SessionId = s }).ToList());
    }
}