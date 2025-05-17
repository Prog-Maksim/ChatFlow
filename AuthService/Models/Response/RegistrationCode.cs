namespace AuthService.Models.Response;

public class RegistrationCode
{
    /// <summary>
    /// Код для подключения двухфакторной аутентификации
    /// </summary>
    public required string Code { get; set; }
}