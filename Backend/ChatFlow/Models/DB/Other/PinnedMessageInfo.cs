namespace ChatFlow.Models.DB.Other;

public class PinnedMessageInfo
{
    /// <summary>
    /// Идентификатор сообщения
    /// </summary>
    public required string MessageId { get; set; }
    
    /// <summary>
    /// Кто закрепил
    /// </summary>
    public required string PinnedBy { get; set; }
    
    /// <summary>
    /// Дата и время закрепления
    /// </summary>
    public DateTime PinnedAt { get; set; } =  DateTime.UtcNow;
}