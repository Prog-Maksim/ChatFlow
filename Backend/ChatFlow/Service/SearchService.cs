using ChatFlow.Enums;
using ChatFlow.Models.Response;
using ChatFlow.Models.Response.SearchObject;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service.Interfaces;

namespace ChatFlow.Service;

public class SearchService: ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private readonly ISearchRepository _repository;
    private readonly JwtTokenService _jwtTokenService;
    
    public SearchService(ILogger<SearchService> logger, ISearchRepository repository, JwtTokenService jwtTokenService)
    {
        _logger = logger;
        _repository = repository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<BaseResponse<string, SearchResult>> SearchAsync(string accessToken, string query)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, SearchResult> { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        var searchResult = await _repository.SearchAsync(query);

        if (searchResult is null)
            return new BaseResponse<string, SearchResult>
            {
                Message = "Поиск ничего не дал",
                Type = ResponseType.SearchNotFound,
                Errors = "Service Unavailable", Status = 503, Successfully = false, Data = null
            };
        
        List<User> users = new ();
        List<Group> groups = new ();
        List<Channel> channels = new ();
        List<Bot> bots = new ();

        foreach (var search in searchResult)
        {
            if (search.Type == ChatType.User)
            {
                User user = new User
                {
                    Name = search.Name!,
                    Surname = search.Surname!,
                    PersonId = search.ChatId,
                    Tag = search.Tag
                };
                users.Add(user);
            }
            else if (search.Type == ChatType.Group)
            {
                Group group = new Group
                {
                    Title = search.Title!,
                    GroupId = search.ChatId,
                };
                groups.Add(group);
            }
            else if (search.Type == ChatType.Channel)
            {
                Channel channel = new Channel()
                {
                    Title = search.Title!,
                    ChannelId = search.ChatId,
                };
                channels.Add(channel);
            }
            else if (search.Type == ChatType.Bot)
            {
                Bot bot = new Bot
                {
                    Title = search.Title!,
                    BotId = search.ChatId,
                    Tag = search.Tag!
                };
                bots.Add(bot);
            }
        }

        Search searchData = new Search
        {
            User = users.Count == 0? null: users,
            Group = groups.Count == 0? null: groups,
            Channel = channels.Count == 0? null: channels,
            Bot = bots.Count == 0? null: bots,
        };

        SearchResult result = new SearchResult
        {
            Query = query,
            Count = searchResult.Count,
            Search = searchData
        };

        return new BaseResponse<string, SearchResult>
        {
            Message = "Результаты поиска",
            Type = ResponseType.Ok,
            Status = 200,
            Successfully = true,
            Errors = null,
            Data = result
        };
    }
}