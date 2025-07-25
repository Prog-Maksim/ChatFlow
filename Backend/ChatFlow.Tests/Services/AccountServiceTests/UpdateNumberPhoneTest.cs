using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using ChatFlow.Service.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChatFlow.Tests.Services.AccountServiceTests;

public class UpdateNumberPhoneTest
{
    private readonly Mock<IAuthRepository> _authRepoMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly Mock<ITokenValidator> _tokenValidatorMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    
    private readonly AccountService _service;

    public UpdateNumberPhoneTest() {
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
        _tokenValidatorMock.Reset();
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("bad_token"))
            .ReturnsAsync((false, null));

        // Act
        var result = await _service.UpdateNumberPhone("bad_token", "1234567890");

        // Assert
        Assert.Equal("JWT токен недействителен", result.Message);
    }

    [Fact]
    public async Task PhoneNumberInUse()
    {
        // Arrange
        var validToken = new JwtTokenData { PersonId = "p1" };

        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("valid_token"))
            .ReturnsAsync((true, validToken));

        _authRepoMock.Setup(x => x.GetUserByPhoneNumberAsync("1234567890"))
            .ReturnsAsync(new Persons{ PersonId = "p1", NumberPhone = "1234567890", RegistrationIp = "", AccountState = AccountState.Active, RegistrationTime = DateTime.UtcNow });

        // Act
        var result = await _service.UpdateNumberPhone("valid_token", "1234567890");

        // Assert
        Assert.Equal("Данный номер телефона занят", result.Message);
    }
    
    [Fact]
    public async Task PersonNotFound()
    {
        // Arrange
        var validToken = new JwtTokenData { PersonId = "p1" };

        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("valid_token"))
            .ReturnsAsync((true, validToken));

        _authRepoMock.Setup(x => x.GetUserByPhoneNumberAsync("1234567890"))
            .ReturnsAsync((Persons?)null);

        _authRepoMock.Setup(x => x.GetUserByIdAsync(validToken.PersonId))
            .ReturnsAsync((Persons?)null);

        // Act
        var result = await _service.UpdateNumberPhone("valid_token", "1234567890");

        // Assert
        Assert.Equal("Пользователь не найден", result.Message);
    }
    
    [Fact]
    public async Task Success()
    {
        // Arrange
        var validToken = new JwtTokenData { PersonId = "p1" };
        var person = new Persons { PersonId = "p1", NumberPhone = "old_number", RegistrationIp = "", AccountState = AccountState.Active, RegistrationTime = DateTime.UtcNow };

        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("valid_token"))
            .ReturnsAsync((true, validToken));

        _authRepoMock.Setup(x => x.GetUserByPhoneNumberAsync("1234567890"))
            .ReturnsAsync((Persons?)null);

        _authRepoMock.Setup(x => x.GetUserByIdAsync("p1"))
            .ReturnsAsync(person);

        _authRepoMock.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateNumberPhone("valid_token", "1234567890");

        // Assert
        Assert.Equal("Номер телефона успешно обновлен", result.Message);
        Assert.Equal("1234567890", person.NumberPhone);
        _authRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}