using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;
using SearchService.Enums;

namespace SearchService.Models.DB;

public class IndexPerson
{
    /// <summary>
    /// Идентификатор чата
    /// </summary>
    public required string ChatId { get; set; }
    
    /// <summary>
    /// Тип чата
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public ChatType Type { get; set; }
    
    /// <summary>
    /// Название чата
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    public string? Surname { get; set; }
    
    /// <summary>
    /// Тег пользователя, бота
    /// </summary>
    public string? Tag { get; set; }
}