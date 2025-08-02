using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using Microsoft.Extensions.Logging;
using Moq;
using ChatUser = ChatFlow.Models.DB.ChatUser;

namespace ChatFlow.Tests.Services.ChatServiceTests;

public class GetChatsTests
{
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<IProfileRepository> _profileRepoMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IWebSocketConnectionManager> _wsManagerMock;
    private readonly Mock<ILogger<ChatService>> _loggerMock;

    private readonly ChatService _service;

    public GetChatsTests()
    {
        _chatRepoMock = new Mock<IChatRepository>();
        _profileRepoMock = new Mock<IProfileRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _wsManagerMock = new Mock<IWebSocketConnectionManager>();
        _loggerMock = new Mock<ILogger<ChatService>>();

        _service = new ChatService(
            _loggerMock.Object,
            _chatRepoMock.Object,
            _jwtTokenServiceMock.Object,
            _profileRepoMock.Object,
            _wsManagerMock.Object
        );
    }
    
    [Fact]
    public async Task JwtTokenInvalid()
    {
        var token = "bad_token";
        var tokenData = new JwtTokenData();

        _jwtTokenServiceMock.Setup(x => x.GetJwtTokenData(token)).Returns(tokenData);
        _jwtTokenServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData)).ReturnsAsync(false);

        var result = await _service.GetChatInfo(token, "chat1");

        Assert.False(result.Successfully);
        Assert.Equal(403, result.Status);
        Assert.Equal(ResponseType.JwtTokenVerificationFailed, result.Type);
    }

    [Fact]
    public async Task ChatNotFound()
    {
        var token = "valid_token";
        var tokenData = new JwtTokenData { PersonId = "user1" };

        _jwtTokenServiceMock.Setup(x => x.GetJwtTokenData(token)).Returns(tokenData);
        _jwtTokenServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(x => x.GetChat("chat1")).ReturnsAsync((ChatDocument)null!);

        var result = await _service.GetChatInfo(token, "chat1");

        Assert.True(result.Successfully); // Странное поведение, но ты так написал
        Assert.Equal(404, result.Status);
        Assert.Equal(ResponseType.ChatNotFound, result.Type);
    }

    [Fact]
    public async Task UserNotInChat()
    {
        var token = "valid_token";
        var tokenData = new JwtTokenData { PersonId = "user1" };

        var chat = new ChatDocument
        {
            ChatId = "chat1",
            Persons = new()
            {
                new ChatUser { PersonId = "other_user", Role = Roles.Owner }
            }
        };

        _jwtTokenServiceMock.Setup(x => x.GetJwtTokenData(token)).Returns(tokenData);
        _jwtTokenServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(x => x.GetChat("chat1")).ReturnsAsync(chat);

        var result = await _service.GetChatInfo(token, "chat1");

        Assert.False(result.Successfully);
        Assert.Equal(403, result.Status);
        Assert.Equal(ResponseType.UserNotInChat, result.Type);
    }
    
    [Fact]
    public async Task UserNotFoundInPrivateChat()
    {
        var token = "valid_token";
        var tokenData = new JwtTokenData { PersonId = "user1" };

        var chat = new ChatDocument
        {
            ChatId = "chat1",
            Type = ChatType.Private,
            Persons = new()
            {
                new ChatUser{ PersonId = "user1", Role = Roles.Owner }
            }
        };

        _jwtTokenServiceMock.Setup(x => x.GetJwtTokenData(token)).Returns(tokenData);
        _jwtTokenServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(x => x.GetChat("chat1")).ReturnsAsync(chat);

        var result = await _service.GetChatInfo(token, "chat1");

        Assert.False(result.Successfully);
        Assert.Equal(404, result.Status);
        Assert.Equal(ResponseType.PersonNotFound, result.Type);
    }

    [Fact]
    public async Task ChatInfoForPrivateChat()
    {
        var token = "valid_token";
        var tokenData = new JwtTokenData { PersonId = "user1" };

        var chat = new ChatDocument
        {
            ChatId = "chat1",
            Type = ChatType.Private,
            Persons = new()
            {
                new ChatUser{ PersonId = "user1", Role = Roles.Owner }, 
                new ChatUser{ PersonId = "user2", Role = Roles.User }
            }
        };

        _jwtTokenServiceMock.Setup(x => x.GetJwtTokenData(token)).Returns(tokenData);
        _jwtTokenServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(x => x.GetChat("chat1")).ReturnsAsync(chat);

        _profileRepoMock.Setup(x => x.GetSummaryPersonDataAsync("user2")).ReturnsAsync(new SummaryDataPerson
        {
            Name = "Ivan",
            Surname = "Ivanov",
            Image = new DataImage { ImageId = "12345", Url = "https://12345.jpg" }
        });

        var result = await _service.GetChatInfo(token, "chat1");

        Assert.True(result.Successfully);
        Assert.Equal(200, result.Status);
        Assert.Equal("Ivanov Ivan", result.Data.Title);
        Assert.Equal("https://12345.jpg", result.Data.ImageUrl);
    }

    [Fact]
    public async Task ChatInfoForGroupChat()
    {
        var token = "valid_token";
        var tokenData = new JwtTokenData { PersonId = "user1" };

        var chat = new ChatDocument
        {
            ChatId = "chat1",
            Type = ChatType.Group,
            Title = "Team",
            PhotoId = "group.png",
            Persons = new()
            {
                new ChatUser{ PersonId = "user1", Role = Roles.Owner }, 
                new ChatUser{ PersonId = "user2", Role = Roles.User }
            }
        };

        _jwtTokenServiceMock.Setup(x => x.GetJwtTokenData(token)).Returns(tokenData);
        _jwtTokenServiceMock.Setup(x => x.ValidateJwtAccessToken(tokenData)).ReturnsAsync(true);
        _chatRepoMock.Setup(x => x.GetChat("chat1")).ReturnsAsync(chat);

        var result = await _service.GetChatInfo(token, "chat1");

        Assert.True(result.Successfully);
        Assert.Equal(200, result.Status);
        Assert.Equal("Team", result.Data.Title);
        Assert.Equal("group.png", result.Data.ImageUrl);
    }
}