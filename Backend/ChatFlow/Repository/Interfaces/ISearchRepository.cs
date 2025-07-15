using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using Nest;

namespace ChatFlow.Repository.Interfaces;

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
    /// <param name="size">Кол-во значений в выборке</param>
    /// <returns></returns>
    /// <exception cref="Exception">Ошибка выполнения поиска</exception>
    public Task<IReadOnlyCollection<IndexPerson>?> SearchAsync(string query, int? size = 20);

    /// <summary>
    /// Обновляет данные пользователя
    /// </summary>
    /// <param name="data">Обновленные данные пользователя</param>
    /// <returns></returns>
    public Task<bool> UpdatePersonAsync(UserCreated data);
}