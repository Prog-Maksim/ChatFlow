using ChatFlow.Models.DB;

namespace ChatFlow.Models.Response;

public class SendMessage
{
    /// <summary>
    /// Объект сообщения
    /// </summary>
    public required MessageData Message { get; set; }
}