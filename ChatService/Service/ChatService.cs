using ChatService.Enums;
using ChatService.Models.DB;
using ChatService.Models.Events;
using ChatService.Models.Response;
using ChatService.Repository.Interfaces;
using ChatService.Scripts;

namespace ChatService.Service;

public class ChatService
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<ChatService> _logger;
    private readonly IChatRepository _chatRepository;
    private readonly KafkaEventProducer _kafkaEventProducer;

    public ChatService(ILogger<ChatService> logger, IChatRepository chatRepository, JwtTokenService jwtTokenService, KafkaEventProducer kafkaEventProducer)
    {
        _logger = logger;
        _chatRepository = chatRepository;
        _jwtTokenService = jwtTokenService;
        _kafkaEventProducer = kafkaEventProducer;
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
        await CreateMessageAddChat(chatData);
        
        return new BaseResponse<string, string>
        {
            Message = "Создан чат", Type = ResponseType.Ok, Successfully = true, Status = 200, Errors = null,
            Data = chatData.ChatId
        };
    }

    private async Task CreateMessageAddChat(ChatDocument message)
    {
        _logger.LogInformation("Подготовка отправки события о создании чата");
        ChatCreated chatData = new ChatCreated
        {
            ChatId = message.ChatId,
            Users = message.Persons
        };
        await _kafkaEventProducer.PublishUserCreatedAsync(chatData);
    }
}