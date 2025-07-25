using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using ChatFlow.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChatFlow.Tests.Services.AccountServiceTests;

public class UpdatePasswordTest
{
    private readonly Mock<IAuthRepository> _authRepoMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly Mock<ITokenValidator> _tokenValidatorMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    
    private readonly AccountService _service;

    public UpdatePasswordTest()
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
    public async Task IpIsBlocked()
    {
        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(true);

        var result = await _service.UpdatePassword("token", "old", "new", "1.1.1.1");

        Assert.Equal("Слишком много попыток. Попробуйте еще раз позже.", result.Message);
    }

    [Fact]
    public async Task JwtTokenInvalid()
    {
        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(false);
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("token")).ReturnsAsync((false, null));

        var result = await _service.UpdatePassword("token", "old", "new", "1.1.1.1");

        Assert.Equal("JWT токен недействителен", result.Message);
    }

    [Fact]
    public async Task PersonNotFound()
    {
        var tokenData = new JwtTokenData { PersonId = "p1" };

        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(false);
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("token")).ReturnsAsync((true, tokenData));
        _authRepoMock.Setup(x => x.GetUserByIdAsync("p1")).ReturnsAsync((Persons?)null);

        var result = await _service.UpdatePassword("token", "old", "new", "1.1.1.1");

        Assert.Equal("Пользователь не найден", result.Message);
    }

    [Fact]
    public async Task PasswordHashIsNull()
    {
        var person = new Persons { PersonId = "p1", PasswordHash = null, NumberPhone = "1234567890", RegistrationIp = "", AccountState = AccountState.Active, RegistrationTime = DateTime.UtcNow };

        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(false);
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("token")).ReturnsAsync((true, new JwtTokenData { PersonId = "p1" }));
        _authRepoMock.Setup(x => x.GetUserByIdAsync("p1")).ReturnsAsync(person);

        var result = await _service.UpdatePassword("token", "old", "new", "1.1.1.1");

        Assert.Equal("Пароль не найден", result.Message);
    }

    [Fact]
    public async Task PasswordIncorrect()
    {
        var person = new Persons { PersonId = "p1", PasswordHash = null, NumberPhone = "1234567890", RegistrationIp = "", AccountState = AccountState.Active, RegistrationTime = DateTime.UtcNow };
        person.PasswordHash = new PasswordHasher<Persons>().HashPassword(person, "hashed");
        
        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(false);
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("token")).ReturnsAsync((true, new JwtTokenData { PersonId = "p1" }));
        _authRepoMock.Setup(x => x.GetUserByIdAsync("p1")).ReturnsAsync(person);
        _authRepoMock.Setup(x => x.IncrementLoginAttemptsAsync("1.1.1.1")).Returns(Task.CompletedTask);

        var service = new AccountService(
            _authRepoMock.Object,
            _encryptionMock.Object,
            _tokenValidatorMock.Object,
            _sessionServiceMock.Object,
            _loggerMock.Object
        );
        
        typeof(AccountService)
            .GetField("_passwordHasher", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(service, new PasswordHasher<Persons>());

        var result = await service.UpdatePassword("token", "wrong", "new", "1.1.1.1");

        Assert.Equal("Данный пароль не верен", result.Message);
    }

    [Fact]
    public async Task NewPassword_Equals_OldPassword()
    {
        var person = new Persons { PersonId = "p1", PasswordHash = null, NumberPhone = "1234567890", RegistrationIp = "", AccountState = AccountState.Active, RegistrationTime = DateTime.UtcNow };
        person.PasswordHash = new PasswordHasher<Persons>().HashPassword(person, "same");

        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(false);
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("token")).ReturnsAsync((true, new JwtTokenData { PersonId = "p1" }));
        _authRepoMock.Setup(x => x.GetUserByIdAsync("p1")).ReturnsAsync(person);

        var result = await _service.UpdatePassword("token", "same", "same", "1.1.1.1");

        Assert.Equal("Данный пароль уже используется", result.Message);
    }

    [Fact]
    public async Task AccountNotActivated()
    {
        var person = new Persons { PersonId = "p1", PasswordHash = null, NumberPhone = "1234567890", RegistrationIp = "", AccountState = AccountState.Registration, RegistrationTime = DateTime.UtcNow };
        person.PasswordHash = new PasswordHasher<Persons>().HashPassword(person, "old");

        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(false);
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("token")).ReturnsAsync((true, new JwtTokenData { PersonId = "p1" }));
        _authRepoMock.Setup(x => x.GetUserByIdAsync("p1")).ReturnsAsync(person);
        _authRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        
        // typeof(AccountService)
        //     .GetMethod("GenerateRegistrationCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        //     ?.Invoke(_service, null);

        // временно можно смоделировать через try/catch в основном методе

        // Act
        var result = await _service.UpdatePassword("token", "old", "new", "1.1.1.1");

        // Assert
        Assert.Equal("Требуется сначала завершить регистрацию аккаунта", result.Message);
    }

    [Fact]
    public async Task Success()
    {
        var person = new Persons { PersonId = "p1", PasswordHash = null, NumberPhone = "1234567890", RegistrationIp = "", AccountState = AccountState.Active, RegistrationTime = DateTime.UtcNow, TotpCode = "dfd"};
        person.PasswordHash = new PasswordHasher<Persons>().HashPassword(person, "old");

        _authRepoMock.Setup(x => x.IsBlockedAsync("1.1.1.1")).ReturnsAsync(false);
        _tokenValidatorMock.Setup(x => x.TryValidateTokenAsync("token")).ReturnsAsync((true, new JwtTokenData { PersonId = "p1" }));
        _authRepoMock.Setup(x => x.GetUserByIdAsync("p1")).ReturnsAsync(person);
        _authRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(x => x.RevokeSession("token", "session")).ReturnsAsync(ResponseFactory.Success("Успешно", new List<RevokeSession>{}));

        // Подделка генерации кода (можно было бы вынести в сервис)
        // typeof(AccountService)
        //     .GetMethod("GenerateRegistrationCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        //     ?.Invoke(_service, null);

        var result = await _service.UpdatePassword("token", "old", "new", "1.1.1.1");

        Assert.Equal("Остался всего один шаг", result.Message);
    }
}