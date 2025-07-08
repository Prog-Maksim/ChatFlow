using System.ComponentModel.DataAnnotations;

namespace ProfileService.Models.DB;

public class Images
{
    public int Id { get; set; }
    
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [StringLength(36)]
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Идентификатор изображения
    /// </summary>
    public required string ImageId { get; set; }
    
    /// <summary>
    /// Основное изображение?
    /// </summary>
    public bool IsPrimary { get; set; } = false;
    
    /// <summary>
    /// Дата и время добавления изображения
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    public Persons? Person { get; set; }
}