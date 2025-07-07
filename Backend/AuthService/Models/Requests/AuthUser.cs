using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Requests;

public class AuthUser
{
    /// <summary>
    /// Логин
    /// </summary>
    public required string Login { get; set; }
    
    /// <summary>
    /// Пароль
    /// </summary>
    [StringLength(50, MinimumLength = 10, ErrorMessage = "Поле 'Password' должен быть в пределах от 10 до 50 символов")]
    public required string Password { get; set; }
}