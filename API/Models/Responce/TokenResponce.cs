namespace API.Models.Responce;

public class TokenResponse: BaseResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}