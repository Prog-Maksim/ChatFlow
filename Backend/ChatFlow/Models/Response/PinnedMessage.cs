namespace ChatFlow.Models.Response;

public class PinnedMessage
{
    /// <summary>
    /// Кол-во закрепленных сообщений
    /// </summary>
    public int CountPinnedMessage { get; set; }
    
    /// <summary>
    /// Идентификатор закрепленных сообщений
    /// </summary>
    public List<string>? Messages { get; set; }
}