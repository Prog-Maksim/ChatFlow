namespace AuthService.Models.Other;

public class PersonRegion
{
    /// <summary>
    /// Страна нахождения пользователя
    /// </summary>
    public required string Country { get; set; }
    
    /// <summary>
    /// Город нахождения пользователя
    /// </summary>
    public required string City { get; set; }
}