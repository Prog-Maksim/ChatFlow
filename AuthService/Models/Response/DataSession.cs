namespace AuthService.Models.Response;

public class DataSession
{
    /// <summary>
    /// Кол-во сессий
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// Список сессий
    /// </summary>
    public List<Sessions>? Sessions { get; set; }
}