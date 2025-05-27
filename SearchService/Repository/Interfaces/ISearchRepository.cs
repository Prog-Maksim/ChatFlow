using SearchService.Models.DB;
using SearchService.Models.Events;

namespace SearchService.Repository.Interfaces;

public interface ISearchRepository
{
    /// <summary>
    /// Добавляет пользователя в индексацию ElasticSearch
    /// </summary>
    /// <param name="user">Объект пользователя</param>
    /// <returns></returns>
    public Task CreatePersonAsync(UserCreated user);
    
    
    /// <summary>
    /// Сохранение
    /// </summary>
    /// <returns></returns>
    public Task SaveChangesAsync();
}