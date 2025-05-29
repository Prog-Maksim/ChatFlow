namespace SearchService.Models.Response.SearchObject;

public class Group
{
    /// <summary>
    /// Название группы
    /// </summary>
    public required string Title { get; set; }
    
    /// <summary>
    /// Идентификатор группы
    /// </summary>
    public required string GroupId { get; set; }
}