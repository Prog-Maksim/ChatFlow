namespace ChatFlow.Models.Response.SearchObject;

public class Bot
{
    /// <summary>
    /// Название бота
    /// </summary>
    public required string Title { get; set; }
    
    /// <summary>
    /// Тег бота
    /// </summary>
    public required string Tag { get; set; }
    
    /// <summary>
    /// Идентификатор бота
    /// </summary>
    public required string BotId { get; set; }
}