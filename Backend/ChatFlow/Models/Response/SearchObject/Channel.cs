namespace ChatFlow.Models.Response.SearchObject;

public class Channel
{
    /// <summary>
    /// Название канала
    /// </summary>
    public required string Title { get; set; }
    
    /// <summary>
    /// Идентификатор канала
    /// </summary>
    public required string ChannelId { get; set; }
}