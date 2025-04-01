namespace AuthService.Models.Response;

public class AuthTokens: BaseResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}