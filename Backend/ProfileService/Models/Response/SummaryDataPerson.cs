using System.Text.Json.Serialization;
using ProfileService.Models.Response;

namespace ProfileService.Models.Other;

public class SummaryDataPerson
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
    /// Ссылка на основную фотографию профиля
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DataImage? Image { get; set; }
}