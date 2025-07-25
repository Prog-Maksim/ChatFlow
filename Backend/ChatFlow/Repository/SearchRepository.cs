using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Other;
using ChatFlow.Repository.Interfaces;
using Nest;

namespace ChatFlow.Repository;

public class SearchRepository: ISearchRepository
{
    private readonly IElasticClient _elasticClient;
    private const string IndexName = "chats";
    private readonly ILogger<SearchRepository> _logger;

    public SearchRepository(IElasticClient elasticClient, ILogger<SearchRepository> logger)
    {
        _elasticClient = elasticClient;
        _logger = logger;
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
            Tag = user.Tag
        };
        
        var response = await _elasticClient.IndexAsync(indexPerson, idx => idx.Index(IndexName).Id(indexPerson.ChatId));

        if (!response.IsValid)
        {
            throw new Exception($"Ошибка индексирования пользователя: {response.ServerError?.Error.Reason}");
        }
    }

    public async Task<IReadOnlyCollection<IndexPerson>?> SearchAsync(string query, int? size = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<IndexPerson>();

        var isTagSearch = query.StartsWith("@");

        ISearchResponse<IndexPerson> searchResponse = await _elasticClient.SearchAsync<IndexPerson>(s => s
            .Index(IndexName)
            .Size(size)
            .Query(q =>
                isTagSearch
                    ? q.Bool(b => b
                        .Must(m => m
                            .Match(mp => mp
                                .Field(f => f.Tag)
                                .Query(query.TrimStart('@'))
                            )
                        )
                        .Filter(f => f.Term(t => t.Type, ChatType.User)
                                     || f.Term(t => t.Type, ChatType.Bot))
                    )
                    : q.MultiMatch(mm => mm
                            .Query(query)
                            .Fields(f => f
                                .Field(p => p.Title)
                                .Field(p => p.Name)
                                .Field(p => p.Surname)
                            )
                            .Type(TextQueryType.MostFields)
                            .Fuzziness(Fuzziness.Auto)
                            .Operator(Operator.Or)
                    )
            )
        );

        if (!searchResponse.IsValid)
        {
            _logger.LogError(searchResponse.ServerError?.Error.Reason);
            return null;
        }

        return searchResponse.Documents;
    }

    public async Task<bool> UpdatePersonAsync(UserCreated data)
    {
        var response = await _elasticClient.UpdateAsync<IndexPerson>(
            id: data.PersonId,
            selector: u => u
                .Index("chats")
                .Doc(new IndexPerson
                {
                    ChatId = data.PersonId,
                    Name = data.Name,
                    Surname = data.Surname,
                    Tag = data.Tag
                })
                .DocAsUpsert(false)
        );

        if (!response.IsValid)
        {
            Console.WriteLine($"Ошибка при обновлении: {response.ServerError?.Error?.Reason}");
        }

        _logger.LogInformation("Данные пользователя успешно обновлены");
        return response.IsValid;
    }
}