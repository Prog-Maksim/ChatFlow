using Nest;
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
    /// Производит поиск чатов
    /// </summary>
    /// <param name="query">Поисковой запрос</param>
    /// <returns></returns>
    /// <exception cref="Exception">Ошибка выполнения поиска</exception>
    public Task<IReadOnlyCollection<IndexPerson>?> SearchAsync(string query, int? size = 20);

    /// <summary>
    /// Обновляет данные пользователя
    /// </summary>
    /// <param name="data">Обновленные данные пользователя</param>
    /// <returns></returns>
    public Task<bool> UpdatePersonAsync(UserCreated data);
    
    /// <summary>
    /// Сохранение
    /// </summary>
    /// <returns></returns>
    public Task SaveChangesAsync();
}