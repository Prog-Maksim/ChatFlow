using Nest;
using SearchService.Enums;
using SearchService.Models.DB;
using SearchService.Models.Events;
using SearchService.Repository.Interfaces;

namespace SearchService.Repository;

public class SearchRepository: ISearchRepository
{
    private readonly IElasticClient _elasticClient;
    private const string IndexName = "chats";

    public SearchRepository(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }
    
    public async Task CreatePersonAsync(UserCreated user)
    {
        var indexPerson = new IndexPerson
        {
            ChatId = user.PersonId,
            Type = ChatType.User,
            Name = user.Name,
            Surname = user.Surname,
            Title = null,
            Tag = null
        };
        
        var response = await _elasticClient.IndexAsync(indexPerson, idx => idx.Index(IndexName).Id(indexPerson.ChatId));

        if (!response.IsValid)
        {
            throw new Exception($"Ошибка индексирования пользователя: {response.ServerError?.Error.Reason}");
        }
    }
    
    public async Task SaveChangesAsync()
    {
        
    }
}