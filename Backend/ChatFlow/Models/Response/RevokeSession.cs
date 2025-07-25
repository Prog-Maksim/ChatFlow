namespace ChatFlow.Models.Response;

public class RevokeSession
{
    /// <summary>
    /// Идентификатор отозванной сессии
    /// </summary>
    public required string SessionId { get; set; }
}