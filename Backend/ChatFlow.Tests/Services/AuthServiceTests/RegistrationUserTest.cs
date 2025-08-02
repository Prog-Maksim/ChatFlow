using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ChatFlow.Tests.Services.AuthServiceTests;

public class RegistrationUserTest
{
    private readonly Mock<IAuthRepository> _authRepoMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly Mock<ISearchRepository> _searchRepoMock;

    private readonly AuthService _service;
    
    public RegistrationUserTest()
    {
        _authRepoMock = new Mock<IAuthRepository>();
        _encryptionMock = new Mock<IEncryptionService>();
        _searchRepoMock = new Mock<ISearchRepository>();

        _service = new AuthService(
            _authRepoMock.Object,
            _encryptionMock.Object,
            _searchRepoMock.Object
        );
    }
    
    [Fact]
    public async Task IpIsBlocked()
    {
        _authRepoMock.Setup(x => x.IsBlockedAsync("127.0.0.1"))
            .ReturnsAsync(true);

        var result = await _service.AuthorizationUserAsync("1234567890", "password", "127.0.0.1");

        Assert.Equal(ResponseType.TooManyRequests, result.Type);
    }

    [Fact]
    public async Task EmailLoginNotSupported()
    {
        _authRepoMock.Setup(x => x.IsBlockedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.AuthorizationUserAsync("test@mail.com", "password", "127.0.0.1");

        Assert.Equal(ResponseType.EmailLoginNotSupported, result.Type);
    }

    [Fact]
    public async Task LoginIsInvalid()
    {
        _authRepoMock.Setup(x => x.IsBlockedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.AuthorizationUserAsync("invalid_login", "password", "127.0.0.1");

        Assert.Equal(ResponseType.InvalidString, result.Type);
        Assert.Equal("Некорректный логин", result.Message);
    }
    
    [Fact]
    public async Task Success()
    {
        var totp = "encrypted-totp";
        var decryptedTotp = "decrypted-totp";
        var person = new Persons
        {
            NumberPhone = "81234567890",
            RegistrationIp = "",
            RegistrationTime = DateTime.UtcNow,
            AccountState = AccountState.Active,
            TotpCode = totp,
            PersonId = "p1"
        };
        person.PasswordHash = new PasswordHasher<Persons>().HashPassword(person, "hashed");

        _authRepoMock.Setup(x => x.IsBlockedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _authRepoMock.Setup(x => x.GetUserByPhoneNumberAsync("81234567890"))
            .ReturnsAsync(person);

        _encryptionMock.Setup(x => x.Decrypt(totp))
            .Returns(decryptedTotp);

        _authRepoMock.Setup(x => x.GenerateCodeAndSaveAsync(person, "127.0.0.1", decryptedTotp))
            .ReturnsAsync("1234");

        var result = await _service.AuthorizationUserAsync("81234567890", "hashed", "127.0.0.1");
        
        Assert.Equal(ResponseType.Ok, result.Type);
        Assert.Equal("Последний шаг, подтвердите личность", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("1234", result.Data!.Code);
    }
}