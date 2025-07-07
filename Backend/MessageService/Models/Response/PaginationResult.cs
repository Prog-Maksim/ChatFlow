using System.Text.Json.Serialization;

namespace MessageService.Models.Response;

public class PaginationResult
{
    /// <summary>
    /// Общее кол-во сообщений в чате
    /// </summary>
    public required long TotalCount { get; set; }
    
    /// <summary>
    /// Кол-во сообщений в этой выдаче
    /// </summary>
    public required int ReturnedCount { get; set; }
    
    /// <summary>
    /// Лимит сообщений в выдаче
    /// </summary>
    public required int Limit { get; set; }
    
    /// <summary>
    /// Смещение
    /// </summary>
    public int Offset { get; set; }
    
    /// <summary>
    /// Смещение для следующего запроса
    /// </summary>
    public int? NextOffset { get; set; }
}