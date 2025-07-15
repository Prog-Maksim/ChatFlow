using System.ComponentModel.DataAnnotations;

namespace ChatFlow.Models.Requests;

public class UpdatePassword
{
    /// <summary>
    /// Старый пароль
    /// </summary>
    [StringLength(50, MinimumLength = 10, ErrorMessage = "Поле 'OldPassword' должен быть в пределах от 10 до 50 символов")]
    public required string OldPassword { get; set; }
    
    /// <summary>
    /// Новый пароль
    /// </summary>
    [StringLength(50, MinimumLength = 10, ErrorMessage = "Поле 'NewPassword' должен быть в пределах от 10 до 50 символов")]
    public required string NewPassword { get; set; }
}