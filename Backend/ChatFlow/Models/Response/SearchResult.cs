using ChatFlow.Models.Response.SearchObject;

namespace ChatFlow.Models.Response;

public class SearchResult
{
    /// <summary>
    /// Поисковой запрос
    /// </summary>
    public required string Query { get; set; }
    
    /// <summary>
    /// Кол-во найденных чатов
    /// </summary>
    public int Count { get; set; }
  
    /// <summary>
    /// Результаты поиска
    /// </summary>
    public required Search Search { get; set; }
}

public class Search
{
    /// <summary>
    /// Найденные пользователи
    /// </summary>
    public List<User>? User { get; set; }
    
    /// <summary>
    /// Найденные группы
    /// </summary>
    public List<Group>? Group { get; set; }
    
    /// <summary>
    /// Найденные каналы
    /// </summary>
    public List<Channel>? Channel { get; set; }
    
    /// <summary>
    /// Найденные боты
    /// </summary>
    public List<Bot>? Bot { get; set; }
}