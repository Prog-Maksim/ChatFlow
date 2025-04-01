using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Requests;

public class RegistrationUser
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    [StringLength(25, MinimumLength = 10, ErrorMessage = "Поле 'Name' должен быть в пределах от 10 до 25 символов")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    [StringLength(25, MinimumLength = 10, ErrorMessage = "Поле 'Surname' должен быть в пределах от 10 до 25 символов")]
    public required string Surname { get; set; }
    
    /// <summary>
    /// Номер телефона
    /// </summary>
    [StringLength(15, ErrorMessage = "Поле 'NUmberPhone' должен быть в пределах до 15 символов")]
    public required string NumberPhone { get; set; }
    
    /// <summary>
    /// Пароль
    /// </summary>
    [StringLength(50, MinimumLength = 10, ErrorMessage = "Поле 'Password' должен быть в пределах от 10 до 50 символов")]
    public required string Password { get; set; }
}