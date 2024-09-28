using System.Text.Json.Serialization;

namespace API.Models.Responce;

public abstract class BaseResponse
{
    [JsonIgnore]
    public bool Success { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ErrorCode { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Error { get; set; }
}