namespace MessageService.Models.Requests;

public class Message
{
    /// <summary>
    /// Идентификатор чата
    /// </summary>
    public required string ChatId { get; set; }
    
    /// <summary>
    /// Текст сообщения
    /// </summary>
    public required string Text { get; set; }
}