using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using Microsoft.Extensions.Logging;
using Moq;
using ChatUser = ChatFlow.Models.DB.ChatUser;

namespace ChatFlow.Tests.Services.ChatServiceTests;

public class GetChatInfoTest
{
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<IProfileRepository> _profileRepoMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IWebSocketConnectionManager> _wsManagerMock;
    private readonly Mock<ILogger<ChatService>> _loggerMock;

    private readonly ChatService _service;

    public GetChatInfoTest()
    {
        _chatRepoMock = new Mock<IChatRepository>();
        _profileRepoMock = new Mock<IProfileRepository>();
        _jwtServiceMock = new Mock<IJwtTokenService>();
        _wsManagerMock = new Mock<IWebSocketConnectionManager>();
        _loggerMock = new Mock<ILogger<ChatService>>();

        _service = new ChatService(
            _loggerMock.Object,
            _chatRepoMock.Object,
            _jwtServiceMock.Object,
            _profileRepoMock.Object,
            _wsManagerMock.Object
        );
    }
    
    [Fact]
    public async Task JwtTokenInvalid()
    {
        // Arrange
        var accessToken = "bad_token";
        var tokenData = new JwtTokenData();

        _jwtServiceMock.Setup(x => x.GetJwtTokenData(accessToken))
            .Returns(tokenData);
        _jwtServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetChats(accessToken);

        // Assert
        Assert.False(result.Successfully);
        Assert.Equal(403, result.Status);
        Assert.Equal(ResponseType.JwtTokenVerificationFailed, result.Type);
    }

    [Fact]
    public async Task ChatNotFound()
    {
        // Arrange
        var accessToken = "valid_token";
        var tokenData = new JwtTokenData { PersonId = "person_1" };

        _jwtServiceMock.Setup(x => x.GetJwtTokenData(accessToken))
            .Returns(tokenData);
        _jwtServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData))
            .ReturnsAsync(true);
        _chatRepoMock.Setup(x => x.GetChats(tokenData.PersonId))
            .ReturnsAsync((List<ChatDocument>)null!);

        // Act
        var result = await _service.GetChats(accessToken);

        // Assert
        Assert.True(result.Successfully);
        Assert.Equal(200, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.Count);
    }

    [Fact]
    public async Task Success()
    {
        // Arrange
        var accessToken = "valid_token";
        var tokenData = new JwtTokenData { PersonId = "person_1" };

        var mockChats = new List<ChatDocument>
        {
            new ChatDocument
            {
                ChatId = "chat1",
                Persons = new() { new ChatUser{ PersonId = "user1", Role = Roles.Owner }, new ChatUser{ PersonId = "user2", Role = Roles.User } },
            }
        };

        _jwtServiceMock.Setup(x => x.GetJwtTokenData(accessToken))
            .Returns(tokenData);
        _jwtServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData))
            .ReturnsAsync(true);
        _chatRepoMock.Setup(x => x.GetChats(tokenData.PersonId))
            .ReturnsAsync(mockChats);

        // Act
        var result = await _service.GetChats(accessToken);

        // Assert
        Assert.True(result.Successfully);
        Assert.Equal(200, result.Status);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Count > 0);
        Assert.Equal("Список чатов", result.Message);
    }
}