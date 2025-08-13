namespace ChatFlow.Models.Requests;

public class ForwardMessageRequest
{
    /// <summary>
    /// Идентификатор чата в который переслать сообщение
    /// </summary>
    public required string ToChatId { get; set; }
    
    /// <summary>
    /// Дополнительное сообщение к пересылаемому(не обязательно)
    /// </summary>
    public Message? Message { get; set; }
}