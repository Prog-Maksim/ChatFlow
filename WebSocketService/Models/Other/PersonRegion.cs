namespace WebSocketService.Models.Other;

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
    
    public required string Latitude { get; set; }
    
    public required string Longitude { get; set; }
}