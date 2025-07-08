using System.ComponentModel.DataAnnotations;

namespace ChatService.Models.DB;

public class Person
{
    /// <summary>
    /// Идентификатор записи
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [StringLength(36)]
    public required string PersonId { get; set; }
}