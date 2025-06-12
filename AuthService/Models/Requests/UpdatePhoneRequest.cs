using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Requests;

public class UpdatePhoneRequest
{
    [Phone]
    public required string PhoneNumber { get; set; }
}
