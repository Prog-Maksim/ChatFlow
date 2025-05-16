namespace AuthService.Models.Response;

public class GoogleAuthentication: BaseResponse
{
    public required string Key { get; set; }
}