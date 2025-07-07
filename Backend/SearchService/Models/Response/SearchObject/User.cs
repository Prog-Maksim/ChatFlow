namespace SearchService.Models.Response.SearchObject;

public class User
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
    
    /// <summary>
    /// Тег пользователя
    /// </summary>
    public string? Tag { get; set; }
}