using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;

namespace ChatFlow.Service;

public class ChatService
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<ChatService> _logger;
    private readonly IChatRepository _chatRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IWebSocketConnectionManager _manager;

    public ChatService(ILogger<ChatService> logger, IChatRepository chatRepository, JwtTokenService jwtTokenService, IProfileRepository profileRepository, IWebSocketConnectionManager manager)
    {
        _logger = logger;
        _chatRepository = chatRepository;
        _profileRepository = profileRepository;
        _jwtTokenService = jwtTokenService;
        _manager = manager;
    }

    public async Task<BaseResponse<string, string>> CreatePrivateChat(string accessToken, string otherPersonId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, string> 
                { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (!await _chatRepository.PersonExistAsync(otherPersonId))
            return new BaseResponse<string, string> 
                { Message = "Невозможно создать чат", Type = ResponseType.PersonNotFound, Successfully = false, Status = 404, Errors = "Not Found", Data = null };
        
        string? chatId = await _chatRepository.GetPrivateChatIdAsync(dataToken.PersonId, otherPersonId);

        if (chatId is not null)
            return new BaseResponse<string, string>
            {
                Message = "Личный чат", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null,
                Data = chatId
            };
        
        ChatDocument chatData = await _chatRepository.CreatePrivateChatAsync(dataToken.PersonId, otherPersonId);
        _ = SendMessageToCreateChatAsync(chatData.Persons, chatData.ChatId);
        
        return new BaseResponse<string, string>
        {
            Message = "Создан чат", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null,
            Data = chatData.ChatId
        };
    }

    private async Task SendMessageToCreateChatAsync(List<ChatUser> users, string chatId)
    {
        foreach (var user in users)
            await _manager.SendMessageCreateChat(chatId, user.PersonId);
    }
    
    public async Task<BaseResponse<string, Chats>> GetChats(string accessToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, Chats> 
                { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};
        
        var chats = await _chatRepository.GetChats(dataToken.PersonId);

        if (chats is null)
            return new BaseResponse<string, Chats>
            {
                Message = "Список чатов",
                Type = ResponseType.Ok,
                Successfully = true,
                Status = 200,
                Errors = null,
                Data = new Chats { Count = 0 }
            };

        Chats chatsResult = GetPersonChats(dataToken.PersonId, chats);
        
        return new BaseResponse<string, Chats>
        {
            Message = "Список чатов",
            Type = ResponseType.Ok,
            Successfully = true,
            Status = 200,
            Errors = null,
            Data = chatsResult
        };
    }

    private Chats GetPersonChats(string personId, List<ChatDocument> chats)
    {
        Chats chatsResult = new Chats { Count = chats.Count };
        
        List<PrivateChat> privateChatsResult = new ();
        List<GroupChat> groupChatsResult = new ();
        List<ChannelChat> channelChatsResult = new ();
        List<Bots> botChatsResult = new ();

        foreach (var chat in chats)
        {
            switch (chat.Type)
            {
                case ChatType.Private:
                    var target = chat.Persons.FirstOrDefault(p => p.PersonId != personId);
                    if (target != null)
                    {
                        privateChatsResult.Add(new PrivateChat
                        {
                            ChatId = chat.ChatId,
                            TargetPersonId = target.PersonId
                        });
                    }
                    break;
                case ChatType.Group:
                    groupChatsResult.Add(MapTo<GroupChat>(chat));
                    break;
                case ChatType.Channel:
                    channelChatsResult.Add(MapTo<ChannelChat>(chat));
                    break;
                case ChatType.Bot:
                    botChatsResult.Add(MapTo<Bots>(chat));
                    break;
            }
        }
        
        if (privateChatsResult.Count is not 0) chatsResult.PrivateChats = privateChatsResult;
        if (groupChatsResult.Count is not 0) chatsResult.GroupChats = groupChatsResult;
        if (channelChatsResult.Count is not 0) chatsResult.ChannelChats = channelChatsResult;
        if (botChatsResult.Count is not 0) chatsResult.BotChats = botChatsResult;
        
        return chatsResult;
    }
    
    private T MapTo<T>(ChatDocument chat) where T : BaseChat, new()
    {
        return new T
        {
            ChatId = chat.ChatId,
            Title = chat.Title,
            ImageUrl = chat.PhotoId
        };
    }

    public async Task<BaseResponse<string, ChatInfo>> GetChatInfo(string accessToken, string chatId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, ChatInfo> 
                { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        var chat = await _chatRepository.GetChat(chatId);
        
        if (chat is null)
            return new BaseResponse<string, ChatInfo>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound,
                Successfully = true,
                Status = 404,
                Errors = null,
                Data = null
            };

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, ChatInfo>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, 
                Errors = "Forbidden", 
                Status = 403,
                Successfully = false, 
                Data = null
            };

        ChatInfo info = new ChatInfo();
        if (chat.Type == ChatType.Private)
        {
            var personId = chat.Persons.FirstOrDefault(p => p.PersonId != dataToken.PersonId);
            
            if (personId is null)
                return new BaseResponse<string, ChatInfo>
                {
                    Message = "Пользователь не найден",
                    Type = ResponseType.PersonNotFound,
                    Errors = "Not Found",
                    Status = 404,
                    Successfully = false,
                    Data = null
                };
            
            var personData = await _profileRepository.GetSummaryPersonDataAsync(personId.PersonId);
            
            info = new ChatInfo
            {
                ChatId = chat.ChatId,
                Title = $"{personData.Surname} {personData.Name}",
                ImageUrl = personData.Image?.Url
            };
        }
        else
        {
            // TODO: исправить идентификатор на ссылку 
            info = new ChatInfo
            {
                ChatId = chat.ChatId,
                Title = chat.Title,
                ImageUrl = chat.PhotoId
            };
        }

        return new BaseResponse<string, ChatInfo>
        {
            Message = "Данные чата",
            Type = ResponseType.Ok,
            Errors = null,
            Status = 200,
            Successfully = true,
            Data = info
        };
    }
}