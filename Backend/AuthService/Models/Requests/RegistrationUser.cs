using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Requests;

public class RegistrationUser
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    [StringLength(35, MinimumLength = 2, ErrorMessage = "Поле 'Name' должен быть в пределах от 2 до 35 символов")]
    [RegularExpression(@"^[^@]*$", ErrorMessage = "Поле 'Name' не должно содержать символ '@'")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    [StringLength(35, MinimumLength = 2, ErrorMessage = "Поле 'Surname' должен быть в пределах от 2 до 35 символов")]
    [RegularExpression(@"^[^@]*$", ErrorMessage = "Поле 'Surname' не должно содержать символ '@'")]
    public required string Surname { get; set; }
    
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