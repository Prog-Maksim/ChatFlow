using System.Text.Json.Serialization;
using ProfileService.Models.Response;

namespace ProfileService.Models.Other;

public class DataPerson
{
    /// <summary>
    /// Имя
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Фамилия
    /// </summary>
    public required string Surname { get; set; }
    
    /// <summary>
    /// Тег пользователя
    /// </summary>
    public string? Tag { get; set; }
    
    /// <summary>
    /// Описание профиля
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Ссылки на фотографии профиля
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DataImage>? Images { get; set; }
    
}