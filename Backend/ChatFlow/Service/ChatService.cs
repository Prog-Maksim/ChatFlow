using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Response;
using ChatFlow.Monitoring;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts.Interfaces;
using ChatFlow.Service.Interfaces;

namespace ChatFlow.Service;

public class ChatService: IChatService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<ChatService> _logger;
    private readonly IChatRepository _chatRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IWebSocketConnectionManager _manager;
    private readonly IMessageRepository _messageRepository;
    private readonly IWebSocketConnectionManager _webSocketConnectionManager;

    public ChatService(ILogger<ChatService> logger, IChatRepository chatRepository, IJwtTokenService jwtTokenService, IProfileRepository profileRepository, IWebSocketConnectionManager manager, IMessageRepository messageRepository,  IWebSocketConnectionManager webSocketConnectionManager)
    {
        _logger = logger;
        _chatRepository = chatRepository;
        _profileRepository = profileRepository;
        _jwtTokenService = jwtTokenService;
        _manager = manager;
        _messageRepository = messageRepository;
        _webSocketConnectionManager = webSocketConnectionManager;
    }

    public async Task<BaseResponse<string, CreateChat>> CreatePrivateChat(string accessToken, string otherPersonId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, CreateChat> 
                { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (!await _chatRepository.PersonExistAsync(otherPersonId))
            return new BaseResponse<string, CreateChat> 
                { Message = "Невозможно создать чат", Type = ResponseType.PersonNotFound, Successfully = false, Status = 404, Errors = "Not Found", Data = null };
        
        string? chatId = await _chatRepository.GetPrivateChatIdAsync(dataToken.PersonId, otherPersonId);

        if (chatId is not null)
        {
            ChatDocument? chat = await _chatRepository.GetChat(chatId);
            if (chat!.HiddenForUsers.Contains(dataToken.PersonId))
            {
                chat.HiddenForUsers.Remove(dataToken.PersonId);
                await _chatRepository.UpdateChatDataAsync(chatId, chat);
            }
            
            return new BaseResponse<string, CreateChat>
            {
                Message = "Личный чат", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null,
                Data = new CreateChat { ChatId = chatId }
            };
        }
        
        ChatDocument chatData = await _chatRepository.CreatePrivateChatAsync(dataToken.PersonId, otherPersonId);
        _ = SendMessageToCreateChatAsync(chatData.Persons, chatData.ChatId);
        MetricsRegistry.ChatCreationCounter.WithLabels("private").Inc();
        
        return new BaseResponse<string, CreateChat>
        {
            Message = "Создан чат", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null,
            Data = new CreateChat { ChatId = chatData.ChatId }
        };
    }

    public async Task<BaseResponse<string, CreateChat>> CreateSecretPrivateChat(string accessToken, string otherPersonId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, CreateChat> 
                { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        if (!await _chatRepository.PersonExistAsync(otherPersonId))
            return new BaseResponse<string, CreateChat> 
                { Message = "Невозможно создать чат", Type = ResponseType.PersonNotFound, Successfully = false, Status = 404, Errors = "Not Found", Data = null };

        string? chatId = await _chatRepository.GetPrivateChatIdAsync(dataToken.PersonId, otherPersonId);
        
        if (chatId is null)
            return new BaseResponse<string, CreateChat>
            {
                Message = "Сначала нужно создать личный чат", Type = ResponseType.ChatNotFound, Successfully = true, Status = 403, Errors = "Forbidden",
                Data = null
            };
        
        ChatDocument data = (await _chatRepository.GetChat(chatId))!;
        
        if (data.HiddenForUsers.Contains(dataToken.PersonId))
            return new BaseResponse<string, CreateChat>
            {
                Message = "Сначала нужно создать личный чат", Type = ResponseType.ChatNotFound, Successfully = true, Status = 403, Errors = "Forbidden",
                Data = null
            };
        
        if (data.Type == ChatType.SecretPrivate)
            return new BaseResponse<string, CreateChat>
            {
                Message = "Секретный чат", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null,
                Data = new CreateChat { ChatId = chatId }
            };
        
        data.Type = ChatType.SecretPrivate;
        
        string oldChatId = data.ChatId;
        string newChatId = Guid.NewGuid().ToString();
        
        data.ChatId = newChatId;
        data.CreatedAt = DateTime.UtcNow;
        
        await _messageRepository.DeleteAllMessageAsync(oldChatId);
        await _chatRepository.UpdateChatDataAsync(oldChatId, data);

        await _manager.SendMessageMigrationChat(oldChatId, newChatId, data);
        MetricsRegistry.ChatCreationCounter.WithLabels("secret").Inc();
        
        return new BaseResponse<string, CreateChat>
        {
            Message = "Секретный чат", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null,
            Data = new CreateChat { ChatId = newChatId}
        };
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
    
    public async Task<BaseResponse<string, ChatInfo>> GetChatInfo(string accessToken, string chatId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return new BaseResponse<string, ChatInfo> 
                { Message = "Не удалось проверить корректность jwt токена", Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403, Successfully = false, Data = null};

        var chat = await _chatRepository.GetChat(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
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

        ChatInfo info;
        if (chat.Type == ChatType.Private || chat.Type == ChatType.SecretPrivate)
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
                Title = $"{personData!.Surname} {personData.Name}",
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

    public async Task<BaseResponse<string, string>> DeleteAllMessages(string accessToken, string chatId, bool isAll = false)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, string>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, string>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");

        bool success;
        if (isAll || chat.Type == ChatType.SecretPrivate)
        {
            success = await _messageRepository.DeleteAllMessageAsync(chatId);
            if (success) _ = _webSocketConnectionManager.SendMessageDeleteHistoryChat(chatId, chat.Persons);
        }
        else
        {
            chat.ClearedMessagesForUsers[dataToken.PersonId] = DateTime.UtcNow;
            await _chatRepository.UpdateChatDataAsync(chatId, chat);
            success = true;
        }

        if (!success)
            return CreateErrorResponse<string, string>("История чата не удалена", ResponseType.MessageNotModified, 400, "Bad Request");
        
        return new BaseResponse<string, string>
        {
            Message = "История чата успешно удалена",
            Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = "Чат очищен"
        };
    }
    
    public async Task<BaseResponse<string, string>> DeleteChat(string accessToken, string chatId, bool isAll = false)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, string>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, string>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");

        if (isAll || chat.Type == ChatType.SecretPrivate)
        {
            await _chatRepository.DeleteChatAsync(chatId);
            _ = _messageRepository.DeleteAllMessageAsync(chatId);
            _ = _manager.SendMessageDeleteChat(chatId, chat.Persons);
        }
        else
        {
            chat.HiddenForUsers.Add(dataToken.PersonId);
            chat.ClearedMessagesForUsers[dataToken.PersonId] = DateTime.UtcNow;
            await _chatRepository.UpdateChatDataAsync(chatId, chat);
        }

        return new BaseResponse<string, string>
        {
            Message = "Чат успешно удален",
            Type = ResponseType.Ok,
            Successfully = true,
            Status = 200,
            Data = chatId
        };
    }

    public async Task<BaseResponse<string, string>> PinnedMessage(string accessToken, string chatId, string messageId, bool isAll = false)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, string>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, string>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");
        
        var message = await _messageRepository.GetMessageByIdAsync(chatId, messageId, dataToken.PersonId);
        
        if (message is null)
            return CreateErrorResponse<string, string>("Сообщение не найдено!", ResponseType.MessageNotFound, 404, "Not Found");
        
        if (isAll)
        {
            foreach (var person in chat.Persons)
            {
                if (!chat.PinnedMessages.ContainsKey(person.PersonId))
                    chat.PinnedMessages[person.PersonId] = new List<PinnedMessageInfo>();

                var pinnedList = chat.PinnedMessages[person.PersonId];

                if (pinnedList.All(p => p.MessageId != messageId))
                {
                    pinnedList.Add(new PinnedMessageInfo
                    {
                        MessageId = messageId,
                        PinnedBy = dataToken.PersonId
                    });
                }
            }
        }
        else
        {
            if (!chat.PinnedMessages.ContainsKey(dataToken.PersonId))
                chat.PinnedMessages[dataToken.PersonId] = new List<PinnedMessageInfo>();

            var pinnedList = chat.PinnedMessages[dataToken.PersonId];

            if (pinnedList.All(p => p.MessageId != messageId))
            {
                pinnedList.Add(new PinnedMessageInfo
                {
                    MessageId = messageId,
                    PinnedBy = dataToken.PersonId
                });
            }
        }
        
        await _chatRepository.UpdateChatDataAsync(chatId, chat);
        
        return new BaseResponse<string, string>
        {
            Message = "Сообщение успешно закреплено",
            Type = ResponseType.Ok,
            Successfully = true,
            Status = 200,
            Data = messageId
        };
    }

    public async Task<BaseResponse<string, string>> UnPinnedMessage(string accessToken, string chatId, string messageId, bool isAll = false)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, string>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, string>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, string>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");
        
        var message = await _messageRepository.GetMessageByIdAsync(chatId, messageId, dataToken.PersonId);
        
        if (message is null)
            return CreateErrorResponse<string, string>("Сообщение не найдено!", ResponseType.MessageNotFound, 404, "Not Found");
        
        if (isAll)
        {
            foreach (var personId in chat.PinnedMessages.Keys.ToList())
            {
                var pinnedList = chat.PinnedMessages[personId];
                pinnedList.RemoveAll(p => p.MessageId == messageId);

                if (pinnedList.Count == 0)
                    chat.PinnedMessages.Remove(personId);
            }
        }
        else
        {
            if (chat.PinnedMessages.TryGetValue(dataToken.PersonId, out var pinnedList))
            {
                pinnedList.RemoveAll(p => p.MessageId == messageId);
                
                if (pinnedList.Count == 0)
                    chat.PinnedMessages.Remove(dataToken.PersonId);
            }
        }
        
        await _chatRepository.UpdateChatDataAsync(chatId, chat);
        
        return new BaseResponse<string, string>
        {
            Message = "Сообщение успешно откреплено",
            Type = ResponseType.Ok,
            Successfully = true,
            Status = 200,
            Data = messageId
        };
    }

    public async Task<BaseResponse<string, PinnedMessage>> GetPinnedMessage(string accessToken, string chatId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, PinnedMessage>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, PinnedMessage>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, PinnedMessage>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");

        if (chat.PinnedMessages.TryGetValue(dataToken.PersonId, out var pinnedList))
        {
            PinnedMessage message = new PinnedMessage
            {
                CountPinnedMessage = pinnedList.Count,
                Messages = pinnedList.Select(p => p.MessageId).ToList(),
            };
            
            return new BaseResponse<string, PinnedMessage>
            {
                Message = "Закрепленные сообщения",
                Type = ResponseType.Ok,
                Successfully = true,
                Status = 200,
                Data = message
            };
        }
        
        return new BaseResponse<string, PinnedMessage>
        {
            Message = "Закрепленные сообщения",
            Type = ResponseType.Ok,
            Successfully = true,
            Status = 200,
            Data = new PinnedMessage
            {
                CountPinnedMessage = 0,
                Messages = new List<string>()
            }
        };
    }

    private BaseResponse<TErrors, TData> CreateErrorResponse<TErrors, TData>(string message, ResponseType type, int status, TErrors errors)
    {
        return new BaseResponse<TErrors, TData>
        {
            Message = message,
            Type = type,
            Status = status,
            Successfully = false,
            Data = default,
            Errors = errors
        };
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
    
    private async Task SendMessageToCreateChatAsync(List<ChatUser> users, string chatId)
    {
        foreach (var user in users)
            await _manager.SendMessageCreateChat(chatId, user.PersonId);
    }
    
    private Chats GetPersonChats(string personId, List<ChatDocument> chats)
    {
        Chats chatsResult = new Chats { Count = chats.Count };
        
        List<PrivateChat> privateChatsResult = new ();
        List<SecretChat> secretChatsResult = new ();
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
                case ChatType.SecretPrivate:
                    var target1 = chat.Persons.FirstOrDefault(p => p.PersonId != personId);
                    if (target1 != null)
                    {
                        secretChatsResult.Add(new SecretChat
                        {
                            ChatId = chat.ChatId,
                            TargetPersonId = target1.PersonId
                        });
                    }
                    break;
                case ChatType.Group:
                    groupChatsResult.Add(MapTo<GroupChat>(chat));
                    break;
                case ChatType.Channel:
                    channelChatsResult.Add(MapTo<ChannelChat>(chat));
                    break;
            }
        }
        
        if (privateChatsResult.Count is not 0) chatsResult.PrivateChats = privateChatsResult;
        if (secretChatsResult.Count is not 0) chatsResult.SecretChats = secretChatsResult;
        if (groupChatsResult.Count is not 0) chatsResult.GroupChats = groupChatsResult;
        if (channelChatsResult.Count is not 0) chatsResult.ChannelChats = channelChatsResult;
        if (botChatsResult.Count is not 0) chatsResult.BotChats = botChatsResult;
        
        return chatsResult;
    }
}