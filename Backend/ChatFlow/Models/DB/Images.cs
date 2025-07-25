using System.ComponentModel.DataAnnotations;

namespace ChatFlow.Models.DB;

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
    [StringLength(36)]
    public required string ImageId { get; set; }
    
    /// <summary>
    /// Основное изображение?
    /// </summary>
    public bool IsPrimary { get; set; }
    
    /// <summary>
    /// Дата и время добавления изображения
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    public DataPersons? DataPersons { get; set; }
}