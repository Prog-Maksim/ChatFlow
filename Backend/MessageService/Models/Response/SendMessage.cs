using MessageService.Models.DB;

namespace MessageService.Models.Response;

public class SendMessage
{
    /// <summary>
    /// Идентификатор отправленного сообщения
    /// </summary>
    public required MessageData Message { get; set; }
}