using AuthService.Enums;
using AuthService.Models.DB;
using AuthService.Models.Requests;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace AuthService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepositoryMock;
    private readonly Mock<IPasswordHasher<Person>> _passwordHasherMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<ILogger<Service.AuthService>> _loggerMock;
    private readonly Service.AuthService _authService;
    
    public AuthServiceTests()
    {
        _authRepositoryMock = new Mock<IAuthRepository>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _loggerMock = new Mock<ILogger<Service.AuthService>>();

        // _authService = new Service.AuthService(
        //     _authRepositoryMock.Object,
        //     _encryptionServiceMock.Object,
        //     _loggerMock.Object
        // );
    }
    
    /// <summary>
    /// Тест на создание нового пользователя
    /// </summary>
    [Fact]
    public async Task RegistrationUserAsync_ShouldReturnSuccess_WhenUserIsNew()
    {
        // Arrange
        var registrationUser = new RegistrationUser { NumberPhone = "1234567890", Password = "securepass", Name = "Name", Surname = "Surname" };
        var ip = "192.168.1.1";

        _authRepositoryMock.Setup(r => r.GetUserByPhoneNumberAsync(registrationUser.NumberPhone))
            .ReturnsAsync((Person)null);

        _authRepositoryMock.Setup(r => r.AddUserAsync(It.IsAny<Person>()))
            .Returns(Task.FromResult(true));

        _authRepositoryMock.Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegistrationUserAsync(registrationUser, ip);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        _authRepositoryMock.Verify(r => r.AddUserAsync(It.IsAny<Person>()), Times.Once);
        _authRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
    
    /// <summary>
    /// Проверяет что метод возвращает ошибку если пользователь уже зарегистрирован
    /// </summary>
    [Fact]
    public async Task RegistrationUserAsync_ShouldReturnFailure_WhenPhoneNumberExists()
    {
        // Arrange
        var registrationUser = new RegistrationUser { NumberPhone = "1234567890", Password = "securepass", Name = "Name", Surname = "Surname" };
        var ip = "192.168.1.1";
        
        var existingPerson = new Person
        {
            PersonId = Guid.NewGuid().ToString(),
            NumberPhone = "1234567890",
            RegistrationIp = ip,
            RegistrationCountry = "Country",
            RegistrationCity = "City",
            AccountState = AccountState.Registration
        };

        _authRepositoryMock.Setup(r => r.GetUserByPhoneNumberAsync(registrationUser.NumberPhone))
            .ReturnsAsync(existingPerson);

        // Act
        var result = await _authService.RegistrationUserAsync(registrationUser, ip);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        _authRepositoryMock.Verify(r => r.AddUserAsync(It.IsAny<Person>()), Times.Never);
        _authRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}