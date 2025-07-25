using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface ISearchService
{
    /// <summary>
    /// Производит поиск людей и чатов
    /// </summary>
    /// <param name="accessToken">Access токен</param>
    /// <param name="query">Поисковой запрос</param>
    public Task<BaseResponse<string, SearchResult>> SearchAsync(string accessToken, string query);
}