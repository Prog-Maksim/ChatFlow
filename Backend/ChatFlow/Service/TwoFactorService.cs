using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service.Interfaces;
using UAParser;
using Sessions = ChatFlow.Models.DB.Sessions;

namespace ChatFlow.Service;

public class TwoFactorService: ITwoFactorService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(IAuthRepository authRepository, IEncryptionService encryptionService, IJwtTokenService jwtTokenService, ILogger<TwoFactorService> logger)
    {
        _authRepository = authRepository;
        _encryptionService = encryptionService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }
    
    public async Task<BaseResponse<string, Token2Fa>> AddGoogleAuthenticatorAsync(string code, string userIpAddress)
    {
        if (!await _authRepository.CheckCodeAsync(code))
            return ResponseFactory.NotFound<Token2Fa>("Данный код не найден", ResponseType.CodeNotFount);
        
        var data = await _authRepository.GetTotpDataByCodeAsync(code);

        if (data == null || data.IpAddress != userIpAddress)
            return ResponseFactory.AccessDenied<Token2Fa>();
            
        if (data.TotpCode != null && !data.IsUpdate)
            return ResponseFactory.Forbidden<Token2Fa>("Код уже был создан", ResponseType.CodeIsCreated);
            
        var secretKey = AuthenticatorService.GenerateKey();
        await _authRepository.UpdateTotpDataByCodeAsync(code, secretKey);
            
        var tokenResult = new Token2Fa { Token = secretKey };
        return ResponseFactory.Success("Ваш код аутентификации", tokenResult);
    }
    
    public async Task<BaseResponse<string, AuthTokens>> CheckGoogleAuthenticatorAsync(string code, string key, string userIpAddress, string userAgent)
    {
        if (!await _authRepository.CheckCodeAsync(code))
            return ResponseFactory.NotFound<AuthTokens>("Данный код не найден", ResponseType.CodeNotFount);
        
        var data = await _authRepository.GetTotpDataByCodeAsync(code);

        if (data == null)
            return ResponseFactory.NotFound<AuthTokens>("Вы не подключили сервис", ResponseType.ServiceNotConnected);

        if (await _authRepository.GetNumberSessionsAsync(data.PersonId) >= 10)
            return ResponseFactory.Forbidden<AuthTokens>("Достигнуто максимальное количество устройств", ResponseType.DeviceLimitReached);
            
        if (data.IpAddress != userIpAddress)
            return ResponseFactory.AccessDenied<AuthTokens>();

        if (data.TotpCode == null)
            return ResponseFactory.NotFound<AuthTokens>("Подключаемый сервис не найден", ResponseType.ServiceConnectedNotFound);
            
        if (!AuthenticatorService.CheckValidKey(key, data.TotpCode))
            return ResponseFactory.Forbidden<AuthTokens>("Код 2FA не верен", ResponseType.CodeIsNotValid);
        
        if (data.TotpCode != null)
            await TryAddTotpCodeAsync(data.PersonId, data.TotpCode);

        var session = CreateSession(data.PersonId, userIpAddress, userAgent);
        await _authRepository.AddSessionAsync(session);
        await _authRepository.SaveChangesAsync();

        var tokens = _jwtTokenService.CreateJwtToken(
            data.PersonId,
            data.PersonData.PasswordVersion,
            session.SessionId,
            session.Id
        );

        await _authRepository.DeleteTotpDataByCodeAsync(code);

        var tokenResult = new AuthTokens
        {
            PersonId = data.PersonId,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(JwtTokenService.AccessTokenLifetimeMinute),
            RefreshTokenExpiration = DateTime.UtcNow.AddDays(JwtTokenService.RefreshTokenLifetimeDay)
        };
        return ResponseFactory.Success("Вы успешно авторизовались", tokenResult);
    }
    
    public async Task<byte[]> GetQrCodeGoogleAuthenticatorAsync(string code, string userIpAddress)
    {
        if (!await _authRepository.CheckCodeAsync(code))
            throw new NullReferenceException("Данный код не найден");
        
        var data = await _authRepository.GetTotpDataByCodeAsync(code);
            
        if (data == null)
            throw new NullReferenceException("Вы не создали подключение");
            
        if (data.IpAddress != userIpAddress)
            throw new UnauthorizedAccessException("Qr-code не может быть создан!");
            
        if (!data.IsRead)
            throw new UnauthorizedAccessException("Qr-code не может быть создан!");

        if (data.TotpCode == null)
            throw new NullReferenceException("Подключаемый сервис не найден");


        var userData = data.PersonData.Email ?? data.PersonData.NumberPhone;
        string url = AuthenticatorService.GenerateUrl(data.TotpCode, userData);
        return AuthenticatorService.GenerateQrCode(url);
    }
    
    /// <summary>
    /// Добавляет totp код
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="totpCode">Totp код</param>
    private async Task TryAddTotpCodeAsync(string personId, string totpCode)
    {
        var person = await _authRepository.GetUserByIdAsync(personId);
        if (person != null)
            await AddTotpCode(person, totpCode);
        else
            _logger.LogInformation("Пользователь не найден для добавления TOTP");
    }
    
    /// <summary>
    /// Создает объект сессии
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="ipAddress">IP адрес</param>
    /// <param name="userAgent">User агенты пользователя</param>
    /// <returns></returns>
    private Sessions CreateSession(string personId, string ipAddress, string userAgent)
    {
        var parser = Parser.GetDefault();
        var clientInfo = parser.Parse(userAgent);
    
        return new Sessions
        {
            PersonId = personId,
            SessionId = Guid.NewGuid().ToString(),
            IpAddress = _encryptionService.Encrypt(ipAddress),
            Device = clientInfo.Device.ToString(),
            Os = clientInfo.OS.ToString(),
            Browser = clientInfo.UA.ToString(),
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            IsRevoked = false
        };
    }
        
    /// <summary>
    /// Добавляет пользователю totp код
    /// </summary>
    /// <param name="person">Объект пользователя</param>
    /// <param name="totpCode">Totp код</param>
    private async Task AddTotpCode(Persons person, string totpCode)
    {
        try
        {
            person.TotpCode = _encryptionService.Encrypt(totpCode);
            person.AccountState = AccountState.Active;
            await _authRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Произошла ошибка {@ex}", ex);
        }
    }
}