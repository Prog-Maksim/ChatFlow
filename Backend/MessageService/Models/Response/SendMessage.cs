using MessageService.Models.DB;

namespace MessageService.Models.Response;

public class SendMessage
{
    /// <summary>
    /// Объект сообщения
    /// </summary>
    public required MessageData Message { get; set; }
}