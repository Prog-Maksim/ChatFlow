using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Requests;

public class UpdatePhoneRequest
{
    /// <summary>
    /// Номер телефона
    /// </summary>
    [Phone]
    public required string PhoneNumber { get; set; }
}
