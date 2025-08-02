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

public class CreatePrivateChatTest
{
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<IProfileRepository> _profileRepoMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IWebSocketConnectionManager> _wsManagerMock;
    private readonly Mock<ILogger<ChatService>> _loggerMock;

    private readonly ChatService _service;

    public CreatePrivateChatTest()
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
        var token = "invalid_token";
        var fakeData = new JwtTokenData { PersonId = "user1", SessionId = "session1" };

        _jwtServiceMock.Setup(s => s.GetJwtTokenData(token)).Returns(fakeData);
        _jwtServiceMock.Setup(s => s.ValidateJwtAccessToken(fakeData)).ReturnsAsync(false);

        // Act
        var result = await _service.CreatePrivateChat(token, "user2");

        // Assert
        Assert.Equal(ResponseType.JwtTokenVerificationFailed, result.Type);
        Assert.Equal(403, result.Status);
        Assert.False(result.Successfully);
    }

    [Fact]
    public async Task PersonNotFound()
    {
        // Arrange
        var token = "token";
        var otherPersonId = "nonexistent";
        var tokenData = new JwtTokenData { PersonId = "user1" };

        _jwtServiceMock.Setup(s => s.GetJwtTokenData(token)).Returns(tokenData);
        _jwtServiceMock.Setup(s => s.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(s => s.PersonExistAsync(otherPersonId)).ReturnsAsync(false);

        // Act
        var result = await _service.CreatePrivateChat(token, otherPersonId);

        // Assert
        Assert.Equal(ResponseType.PersonNotFound, result.Type);
        Assert.Equal(404, result.Status);
        Assert.False(result.Successfully);
    }

    [Fact]
    public async Task ChatAlreadyExists()
    {
        // Arrange
        var token = "token";
        var otherPersonId = "user2";
        var tokenData = new JwtTokenData { PersonId = "user1" };
        var existingChatId = "chat123";

        _jwtServiceMock.Setup(s => s.GetJwtTokenData(token)).Returns(tokenData);
        _jwtServiceMock.Setup(s => s.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(s => s.PersonExistAsync(otherPersonId)).ReturnsAsync(true);
        _chatRepoMock.Setup(s => s.GetPrivateChatIdAsync("user1", "user2")).ReturnsAsync(existingChatId);

        // Act
        var result = await _service.CreatePrivateChat(token, otherPersonId);

        // Assert
        Assert.True(result.Successfully);
        Assert.Equal(ResponseType.Ok, result.Type);
        Assert.Equal(existingChatId, result.Data);
    }

    [Fact]
    public async Task CreatesChat()
    {
        // Arrange
        var token = "token";
        var otherPersonId = "user2";
        var tokenData = new JwtTokenData { PersonId = "user1" };
        var newChatId = "new_chat_123";

        _jwtServiceMock.Setup(s => s.GetJwtTokenData(token)).Returns(tokenData);
        _jwtServiceMock.Setup(s => s.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(s => s.PersonExistAsync(otherPersonId)).ReturnsAsync(true);
        _chatRepoMock.Setup(s => s.GetPrivateChatIdAsync("user1", "user2")).ReturnsAsync((string?)null);
        _chatRepoMock.Setup(s => s.CreatePrivateChatAsync("user1", "user2"))
            .ReturnsAsync(new ChatDocument
            {
                ChatId = newChatId,
                Persons = new() { new ChatUser{ PersonId = "user1", Role = Roles.Owner }, new ChatUser{ PersonId = "user2", Role = Roles.User } }
            });

        // Act
        var result = await _service.CreatePrivateChat(token, otherPersonId);

        // Assert
        Assert.True(result.Successfully);
        Assert.Equal(ResponseType.Ok, result.Type);
        Assert.Equal(newChatId, result.Data);
    }
}