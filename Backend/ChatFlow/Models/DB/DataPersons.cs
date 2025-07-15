using System.ComponentModel.DataAnnotations;
using ChatFlow.Enums;

namespace ChatFlow.Models.DB;

public class DataPersons
{
    public int Id { get; set; }
    
    [StringLength(36)]
    public required string PersonId { get; set; }
    
    [StringLength(35)]
    public required string Name { get; set; }
    
    [StringLength(35)]
    public required string Surname { get; set; }
    
    [StringLength(15)]
    public string? Tag { get; set; }
    
    [StringLength(250)]
    public string? Description { get; set; }
    
    public List<Images> Images { get; set; } = new();
    public Persons? Persons { get; set; }
}