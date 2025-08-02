using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Requests;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using Moq;

namespace ChatFlow.Tests.Services.AuthServiceTests;

public class AuthorizationUserTest
{
    private readonly Mock<IAuthRepository> _authRepoMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly Mock<ISearchRepository> _searchRepoMock;

    private readonly AuthService _service;
    
    public AuthorizationUserTest()
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
    
    private RegistrationUser GetValidUser() => new()
    {
        Name = "Иван",
        Surname = "Иванов",
        Login = "89281594560",
        Password = "securePassword123"
    };

    [Fact]
    public async Task InvalidPhoneNumberFormat()
    {
        // Arrange
        var user = GetValidUser();
        user.Login = "invalid-phone";

        // Act
        var result = await _service.RegistrationUserAsync(user, "127.0.0.1");

        // Assert
        Assert.False(result.Successfully);
        Assert.Equal("Некорректный формат номера телефона", result.Message);
        Assert.Equal(ResponseType.PhoneNumberNotValid, result.Type);
    }

    [Fact]
    public async Task PhoneNumberAlreadyExists()
    {
        // Arrange
        var user = GetValidUser();

        _authRepoMock.Setup(x => x.GetUserByPhoneNumberAsync(user.Login))
            .ReturnsAsync(new Persons { PersonId = "p1", NumberPhone = "89281594560", RegistrationIp = "", AccountState = AccountState.Active, RegistrationTime = DateTime.UtcNow });

        // Act
        var result = await _service.RegistrationUserAsync(user, "127.0.0.1");

        // Assert
        Assert.False(result.Successfully);
        Assert.Equal("Данный номер телефона занят", result.Message);
        Assert.Equal(ResponseType.PhoneNumberInUse, result.Type);
    }

    [Fact]
    public async Task Success()
    {
        // Arrange
        var user = GetValidUser();
        var generatedCode = "123456";

        _authRepoMock.Setup(x => x.GetUserByPhoneNumberAsync(user.Login))
            .ReturnsAsync((Persons?)null);

        _authRepoMock.Setup(x => x.AddUserAsync(It.IsAny<Persons>()))
            .ReturnsAsync(true);

        _authRepoMock.Setup(x => x.AddUserDataAsync(It.IsAny<DataPersons>()))
            .ReturnsAsync(true);

        _authRepoMock.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _authRepoMock.Setup(x => x.GenerateCodeAndSaveAsync(It.IsAny<Persons>(), "127.0.0.1"))
            .ReturnsAsync(generatedCode);

        // Act
        var result = await _service.RegistrationUserAsync(user, "127.0.0.1");

        // Assert
        Assert.True(result.Successfully);
        Assert.Equal("Пользователь успешно создан", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(generatedCode, result.Data!.Code);
    }
}