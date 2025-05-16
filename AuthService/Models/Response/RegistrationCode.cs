namespace AuthService.Models.Response;

public class RegistrationCode: BaseResponse
{
    /// <summary>
    /// Код для подключения двухфакторной аутентификации
    /// </summary>
    public required string Code { get; set; }
}