using AuthService.Models.DB;

namespace AuthService.Models.Other;

public class TotpData
{
    public required string PersonId { get; set; }
    public required Person PersonData { get; set; }
    public string? TotpCode { get; set; }
}