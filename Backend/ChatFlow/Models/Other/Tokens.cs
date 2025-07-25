namespace ChatFlow.Models.Other;

public class Tokens
{
    /// <summary>
    /// Access токен
    /// </summary>
    public required string AccessToken { get; set; }
    
    /// <summary>
    /// Refresh токен
    /// </summary>
    public required string RefreshToken { get; set; }
}