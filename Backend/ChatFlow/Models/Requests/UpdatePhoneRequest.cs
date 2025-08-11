using System.ComponentModel.DataAnnotations;

namespace ChatFlow.Models.Requests;

public class UpdatePhoneRequest
{
    /// <summary>
    /// Номер телефона
    /// </summary>
    [Phone]
    public required string PhoneNumber { get; set; }
}