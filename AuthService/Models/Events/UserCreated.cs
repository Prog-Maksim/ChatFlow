namespace AuthService.Models.Events;

public class UserCreated
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    public required string Surname { get; set; }
}
