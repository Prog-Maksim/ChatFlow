namespace MessageService.Models.Response;

public class SendMessage
{
    /// <summary>
    /// Идентификатор отправленного сообщения
    /// </summary>
    public required string MessageId { get; set; }
}