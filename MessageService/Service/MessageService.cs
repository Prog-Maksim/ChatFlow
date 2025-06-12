using MessageService.Enums;
using MessageService.Models.DB;
using MessageService.Models.Requests;
using MessageService.Models.Response;
using MessageService.Repository.Interfaces;
using MessageService.Scripts;

namespace MessageService.Service;

public class MessageService
{
    private readonly ILogger<MessageService> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly KafkaEventProducer _kafkaEventProducer;

    public MessageService(ILogger<MessageService> logger, IMessageRepository messageRepository, JwtTokenService jwtTokenService, KafkaEventProducer kafkaEventProducer)
    {
        _logger = logger;
        _messageRepository = messageRepository;
        _jwtTokenService = jwtTokenService;
        _kafkaEventProducer = kafkaEventProducer;
    }

    /// <summary>
    /// Создает сообщение и отправляет в чат
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="message">Объект сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, SendMessage>> SendMessageAsync(string accessToken, Message message, CancellationToken cancellationToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            return new BaseResponse<string, SendMessage>
            {
                Message = "Не удалось проверить корректность jwt токена",
                Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        }
        ChatData? chat = await _messageRepository.GetChatAsync(message.ChatId);
        
        if (chat is null)
            return new BaseResponse<string, SendMessage>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Type != ChatType.Private)
        {
            var personRole = chat.Users.FirstOrDefault(p => p.PersonId == dataToken.PersonId);
        
            if (personRole is null || personRole.Role == Roles.User)
                return new BaseResponse<string, SendMessage>
                {
                    Message = "Вам запрещено отправлять сообщения в этот чат",
                    Type = ResponseType.ChatNotFound, Errors = "Forbidden", Status = 403,
                    Successfully = false, Data = null
                };
        }

        MessageData messageData = await CreateMessageAsync(message, dataToken.PersonId, cancellationToken);
        await _kafkaEventProducer.PublishNewMessageAsync(messageData, chat.Users, cancellationToken);
        
        return new BaseResponse<string, SendMessage>
        {
            Message = "Сообщение успешно отправлено",
            Type = ResponseType.Ok, Status = 200,
            Successfully = true, Errors = null, Data = new SendMessage{ MessageId = messageData.MessageId}
        };
    }

    /// <summary>
    /// Создает объект сообщения и сохраняет в БД
    /// </summary>
    /// <param name="message">Данные сообщения</param>
    /// <param name="ownerPersonId">Автор сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    private async Task<MessageData> CreateMessageAsync(Message message, string ownerPersonId, CancellationToken cancellationToken)
    {
        MessageData messageData = new MessageData
        {
            MessageId = Guid.NewGuid().ToString(),
            ChatId = message.ChatId,
            Text = message.Text,
            Created = DateTime.UtcNow,
            MessageType = MessageStatus.Send,
            OwnerId = ownerPersonId
        };

        await _messageRepository.SaveMessageAsync(messageData, cancellationToken);
        return messageData;
    }

    /// <summary>
    /// Выдает все сообщения чата с пагинацией
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="limit">Кол-во сообщений в выдаче</param>
    /// <param name="offset">Смещение от начала списка</param>
    public async Task<BaseResponse<string, MessagesPagination>> GetMessagesChatAsync(string accessToken, string chatId, int limit, int offset)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            return new BaseResponse<string, MessagesPagination>
            {
                Message = "Не удалось проверить корректность jwt токена",
                Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        }
        ChatData? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessagesPagination>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Users.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, MessagesPagination>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };

        var (messages, totalCount) = await _messageRepository.GetMessagesByChatIdAsync(chatId, limit, offset);

        if (messages.Count == 0)
            return new BaseResponse<string, MessagesPagination>
            {
                Message = "Сообщения не найдены!",
                Type = ResponseType.MessageNotFound, Status = 404, Successfully = false, Data = null,
                Errors = "Not Fount"
            };
        
        var nextOffset = offset + messages.Count;
        
        return new BaseResponse<string, MessagesPagination>
        {
            Message = "Сообщения",
            Type = ResponseType.Ok, Status = 200, Successfully = true,
            Data = new MessagesPagination
            {
                Pagination = new PaginationResult
                {
                    TotalCount = totalCount,
                    Limit = limit,
                    Offset = offset,
                    NextOffset = nextOffset >= totalCount ? 0 : nextOffset,
                    ReturnedCount = messages.Count
                },
                Messages = messages
            }
        };
    }

    /// <summary>
    /// Возвращает последнее сообщение чата
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, MessageData>> GetLastMessageAsync(string accessToken, string chatId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            return new BaseResponse<string, MessageData>
            {
                Message = "Не удалось проверить корректность jwt токена",
                Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        }
        ChatData? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Users.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, MessageData>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };

        MessageData? message = await _messageRepository.GetLastMessageAsync(chatId);

        if (message is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщения не найдены!",
                Type = ResponseType.MessageNotFound, Status = 404, Successfully = false, Data = null,
                Errors = "Not Fount"
            };

        return new BaseResponse<string, MessageData>
        {
            Message = "Последнее сообщение чата",
            Type = ResponseType.Ok, Status = 200, Successfully = true,
            Errors = null, Data = message
        };
    }

    /// <summary>
    /// Удаляет сообщение
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, MessageData>> DeleteMessageAsync(string accessToken, string chatId, string messageId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            return new BaseResponse<string, MessageData>
            {
                Message = "Не удалось проверить корректность jwt токена",
                Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        }
        ChatData? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Users.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, MessageData>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        
        if (chat.Type != ChatType.Private && chat.Type != ChatType.Bot && chat.Users.FirstOrDefault(p => p.PersonId != dataToken.PersonId) is { Role: Roles.User })
            return new BaseResponse<string, MessageData>
            {
                Message = "У вас нет прав на выполнение данного действия в чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        
        var message = await _messageRepository.GetMessageByIdAsync(chatId, messageId);
        
        if (message is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение не найдено",
                Type = ResponseType.MessageNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        message.MessageType = MessageStatus.Deleted;
        var success = await _messageRepository.UpdateMessageAsync(message);
        
        if (!success)
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение не удалено",
                Type = ResponseType.MessageNotModified, Errors = "BadRequest", Status = 400,
                Successfully = false, Data = null
            };

        await _kafkaEventProducer.PublishNewMessageAsync(message, chat.Users, CancellationToken.None);
        return null;
    }

    /// <summary>
    /// Обновляет сообщение
    /// </summary>
    /// <param name="accessToken">Токен пользователя</param>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="messageData">Объект обновляемого сообщения</param>
    /// <returns></returns>
    public async Task<BaseResponse<string, MessageData>> UpdateMessageAsync(string accessToken, string chatId, string messageId, MessageData messageData)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
        {
            return new BaseResponse<string, MessageData>
            {
                Message = "Не удалось проверить корректность jwt токена",
                Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        }
        ChatData? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Users.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, MessageData>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        
        if (chat.Type != ChatType.Private && chat.Type != ChatType.Bot && chat.Users.FirstOrDefault(p => p.PersonId != dataToken.PersonId) is { Role: Roles.User })
            return new BaseResponse<string, MessageData>
            {
                Message = "У вас нет прав на выполнение данного действия в чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        
        var message = await _messageRepository.GetMessageByIdAsync(chatId, messageId);
        
        if (message is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение не найдено",
                Type = ResponseType.MessageNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        var success = await _messageRepository.UpdateMessageAsync(messageData);
        
        if (!success)
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение не изменено",
                Type = ResponseType.MessageNotModified, Errors = "BadRequest", Status = 400,
                Successfully = false, Data = null
            };

        await _kafkaEventProducer.PublishNewMessageAsync(messageData, chat.Users, CancellationToken.None);
        return new BaseResponse<string, MessageData>
        {
            Message = "Сообщение успешно изменено",
            Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = messageData
        };
    }
}