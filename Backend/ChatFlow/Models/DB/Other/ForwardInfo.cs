namespace ChatFlow.Models.DB.Other;

public class ForwardInfo
{
    /// <summary>
    /// Идентификатор чата с которого переслали сообщение
    /// </summary>
    public required string OriginalChatId { get; set; }
    
    /// <summary>
    /// Идентификатор сообщения который переслали
    /// </summary>
    public required string OriginalMessageId { get; set; }
    
    /// <summary>
    /// Идентификатор автора сообщения
    /// </summary>
    public required string OriginalSenderId { get; set; }
}