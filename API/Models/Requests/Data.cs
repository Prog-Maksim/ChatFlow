using System.ComponentModel.DataAnnotations;

namespace API.Models.Requests;

public class RegisteredUser
{
    public string Name { get; set; }
    
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Пароль должен быть не менее 10 символов.")]
    public string Password { get; set; }
}