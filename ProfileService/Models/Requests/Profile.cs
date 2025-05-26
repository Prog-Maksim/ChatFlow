using System.ComponentModel.DataAnnotations;

namespace ProfileService.Models.Requests;

public class Profile
{
    [StringLength(35)]
    public required string Name { get; set; }
    
    [StringLength(35)]
    public required string Surname { get; set; }
    
    [StringLength(15)]
    public string? Tag { get; set; }
    
    [StringLength(250)]
    public string? Description { get; set; }
}