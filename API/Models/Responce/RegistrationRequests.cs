using System.Text.Json.Serialization;

namespace API.Models.Responce;

public class RegistrationRequests: BaseResponse
{
    public string message { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int token_expires { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string access_token { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string refresh_token { get; set; }
}