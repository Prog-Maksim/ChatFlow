using System.ComponentModel.DataAnnotations;

namespace ChatFlow.Models.Requests;

public class Profile
{
    [StringLength(35)]
    [RegularExpression(@"^[^@]*$", ErrorMessage = "Поле 'Name' не должно содержать символ '@'")]
    public required string Name { get; set; }
    
    [StringLength(35)]
    [RegularExpression(@"^[^@]*$", ErrorMessage = "Поле 'Surname' не должно содержать символ '@'")]
    public required string Surname { get; set; }
    
    [StringLength(15)]
    [RegularExpression(@"^[^@]*$", ErrorMessage = "Поле 'Tag' не должно содержать символ '@'")]
    public string? Tag { get; set; }
    
    [StringLength(250)]
    public string? Description { get; set; }
}