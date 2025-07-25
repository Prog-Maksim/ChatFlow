using ChatFlow.Models.Other;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using ChatFlow.Service.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChatFlow.Tests.Services.AccountServiceTests;

public class ExitTheSessionTest
{
    private readonly Mock<IAuthRepository> _authRepoMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly Mock<ITokenValidator> _tokenValidatorMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;

    private readonly AccountService _service;

    public ExitTheSessionTest()
    {
        _authRepoMock = new Mock<IAuthRepository>();
        _encryptionMock = new Mock<IEncryptionService>();
        _tokenValidatorMock = new Mock<ITokenValidator>();
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger<AccountService>>();

        _service = new AccountService(
            _authRepoMock.Object,
            _encryptionMock.Object,
            _tokenValidatorMock.Object,
            _sessionServiceMock.Object,
            _loggerMock.Object
        );
    }
    
    [Fact]
    public async Task JwtTokenInvalid()
    {
        // Arrange
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("invalid_token"))
            .ReturnsAsync((false, null));
        
        // Act
        var result = await _service.ExitTheSession("invalid_token");

        // Assert
        Assert.Equal("JWT токен недействителен", result.Message);
    }

    [Fact]
    public async Task Success()
    {
        // Arrange
        var sessionId = "session_123";
        var tokenData = new JwtTokenData
        {
            PersonId = "p1",
            SessionId = sessionId
        };

        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("valid_token"))
            .ReturnsAsync((true, tokenData));

        _sessionServiceMock.Setup(x => x.RevokeSession("valid_token", sessionId))
            .ReturnsAsync(ResponseFactory.Success("Успешно", new List<RevokeSession> { new() { SessionId = sessionId } }));

        // Act
        var result = await _service.ExitTheSession("valid_token");

        // Assert
        Assert.Equal("Пользователь успешно вышел из аккаунта", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(sessionId, result.Data!.SessionId);
    }
}