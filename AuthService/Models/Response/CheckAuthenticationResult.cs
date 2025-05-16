namespace AuthService.Models.Response;

public class CheckAuthenticationResult: BaseResponse
{
    public required bool Result { get; set; }
}