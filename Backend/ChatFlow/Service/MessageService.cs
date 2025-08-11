using System.Security.Cryptography;
using ChatFlow.Enums;
using ChatFlow.Models.DB;
using ChatFlow.Models.DB.Other;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Monitoring;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service.Interfaces;

namespace ChatFlow.Service;

public class MessageService: IMessageService
{
    private readonly ILogger<MessageService> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IWebSocketConnectionManager _manager;
    private readonly IProfileRepository _profileRepository;
    
    private readonly byte[] _aesKey;   // 32 байта
    private readonly byte[] _hmacKey;  // 32 байта

    public MessageService(IConfiguration configuration, ILogger<MessageService> logger, IMessageRepository messageRepository, IChatRepository chatRepository, IJwtTokenService jwtTokenService, IWebSocketConnectionManager manager, IProfileRepository profileRepository)
    {
        _logger = logger;
        _messageRepository = messageRepository;
        _chatRepository = chatRepository;
        _jwtTokenService = jwtTokenService;
        _manager = manager;
        _profileRepository = profileRepository;
        
        _aesKey = Convert.FromBase64String(configuration["MessageEncryption:Key"]!);
        _hmacKey = Convert.FromBase64String(configuration["MessageEncryption:Hmac"]!);
    }
    
    public async Task<BaseResponse<string, SendMessage>> SendMessageAsync(string accessToken, Message message, CancellationToken cancellationToken)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, SendMessage>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(message.ChatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, SendMessage>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");
        
        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, SendMessage>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");

        if (chat.Type is not (ChatType.Private or ChatType.SecretPrivate))
        {
            var personRole = chat.Persons.FirstOrDefault(p => p.PersonId == dataToken.PersonId);
            
            if (personRole is null || personRole.Role == Roles.User)
                return CreateErrorResponse<string, SendMessage>("Вам запрещено отправлять сообщения в этот чат", ResponseType.ChatNotFound, 403, "Forbidden");
        }

        MessageData messageData = CreateMessageAsync(message, dataToken.PersonId);
        
        if (chat.Type == ChatType.Private)
        {
            if (message.Keys is not null || message.Signature is not null)
                return CreateErrorResponse<string, SendMessage>("Использование полей 'Signature' и 'Keys' запрещено для данного типа чата.", ResponseType.InvalidMessageFields, 400, "Bad Request");

            
            var copyMessage = (MessageData)messageData.Clone();
            await SaveMessageAsync(copyMessage, cancellationToken);
        }
        else if (chat.Type == ChatType.SecretPrivate)
        {
            if (message.Keys is null || message.Signature is null)
                return CreateErrorResponse<string, SendMessage>("У сообщения отсутствуют обязательные поля 'Signature' или 'Keys'", ResponseType.MissingRequiredFields, 400, "Bad Request");
                
            await _messageRepository.SaveMessageAsync(messageData, cancellationToken);
        }

        if (chat.Type is ChatType.Private or ChatType.SecretPrivate && chat.HiddenForUsers.Count != 0)
        {
            chat.HiddenForUsers.Clear();
            _ = _chatRepository.UpdateChatDataAsync(chat.ChatId, chat);
        }
        
        _ = _manager.SendMessageToUserAsync(messageData, chat.Persons);
        MetricsRegistry.MessagesSentCounter.Inc();

        return new BaseResponse<string, SendMessage>
        {
            Message = "Сообщение успешно отправлено",
            Type = ResponseType.Ok, Status = 200,
            Successfully = true, Errors = null, Data = new SendMessage { Message = messageData}
        };
    }
    
    public async Task<BaseResponse<string, MessagesPagination>> GetMessagesChatAsync(string accessToken, string chatId, int limit, int offset)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, MessagesPagination>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, MessagesPagination>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, MessagesPagination>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");
        
        List<MessageData> messages;
        long totalCount;
        
        switch (chat.Type)
        {
            case ChatType.Private:
                (messages, totalCount) = await _messageRepository.GetMessagesByChatIdAsync(chatId, limit, offset, dataToken.PersonId);
                if (messages.Count == 0)
                    return CreateErrorResponse<string, MessagesPagination>("Сообщения не найдены!", ResponseType.MessageNotFound, 404, "Not Found");
                messages = DecryptAndVerifyMany(messages);
                break;
            
            case ChatType.SecretPrivate:
                (messages, totalCount) = await _messageRepository.GetMessagesByChatIdAsync(chatId, limit, offset, dataToken.DeviceId);
                if (messages.Count == 0)
                    return CreateErrorResponse<string, MessagesPagination>("Сообщения не найдены!", ResponseType.MessageNotFound, 404, "Not Found");
                break;

            default:
                return CreateErrorResponse<string, MessagesPagination>("Неизвестный тип чата", ResponseType.UnknownChatType, 400, "Bad Request");
        }

        var nextOffset = offset + messages.Count;
        return CreateSuccessResponse(messages, totalCount, limit, offset, nextOffset);
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
    
    private BaseResponse<string, MessagesPagination> CreateSuccessResponse(
        List<MessageData> messages, long totalCount, int limit, int offset, int nextOffset)
    {
        return new BaseResponse<string, MessagesPagination>
        {
            Message = "Сообщения",
            Type = ResponseType.Ok,
            Status = 200,
            Successfully = true,
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
    
    public async Task<BaseResponse<string, MessageData>> GetLastMessageAsync(string accessToken, string chatId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, MessageData>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, MessageData>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, MessageData>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");

        MessageData? message = null;
        if (chat.Type == ChatType.Private)
        {
            message = await _messageRepository.GetLastMessageAsync(chatId, dataToken.PersonId);

            if (message is null)
                return CreateErrorResponse<string, MessageData>("Сообщения не найдены!", ResponseType.MessageNotFound, 404, "Not Found");
            
            message = DecryptAndVerify(message);
        }
        if (chat.Type == ChatType.SecretPrivate)
        {
            message = await _messageRepository.GetLastMessageAsync(chatId, dataToken.DeviceId);

            if (message is null)
                return CreateErrorResponse<string, MessageData>("Сообщения не найдены!", ResponseType.MessageNotFound, 404, "Not Found");
        }
        
        return new BaseResponse<string, MessageData>
        {
            Message = "Последнее сообщение чата",
            Type = ResponseType.Ok, Status = 200, Successfully = true,
            Errors = null, Data = message
        };
    }
    
    public async Task<BaseResponse<string, MessageData>> DeleteMessageAsync(string accessToken, string chatId, string messageId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, MessageData>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");
        
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, MessageData>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, MessageData>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");
        
        var message = await _messageRepository.GetMessageByIdAsync(chatId, messageId, dataToken.PersonId);
        
        if (message is null)
            return CreateErrorResponse<string, MessageData>("Сообщение не найдено!", ResponseType.MessageNotFound, 404, "Not Found");
        
        bool success = await _messageRepository.DeleteMessageAsync(chatId, messageId);
        
        if (!success)
            return CreateErrorResponse<string, MessageData>("Сообщение не удалено", ResponseType.MessageNotModified, 400, "Bad Request");

        _ = _manager.SendMessageDeleteMessageAsync(message, chat.Persons);
        return new BaseResponse<string, MessageData>
        {
            Message = "Сообщение успешно удалено",
            Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = null
        };
    }
    
    public async Task<BaseResponse<string, MessageData>> UpdateMessageAsync(string accessToken, string chatId, string messageId, UpdateMessage messageData)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, MessageData>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, MessageData>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, MessageData>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");
        
        var message = await _messageRepository.GetMessageByIdAsync(chatId, messageId, dataToken.PersonId);
        
        if (message is null)
            return CreateErrorResponse<string, MessageData>("Сообщение не найдено!", ResponseType.MessageNotFound, 404, "Not Found");
        
        if (chat.Type == ChatType.Private)
        {
            MessageData? updateMessage = DecryptAndVerify(message);
            
            if(updateMessage is null)
                return CreateErrorResponse<string, MessageData>("Сообщение не изменено", ResponseType.MessageNotModified, 400, "Bad Request");
            
            updateMessage.Text = messageData.Text;
            updateMessage.MessageType = MessageStatus.Updated;
            updateMessage.Updated = DateTime.UtcNow;
        
            (string EncryptedText, string IV, string Hmac) dataEncrypt = EncryptAndSign(updateMessage.Text);
        
            var messageCopy = (MessageData)updateMessage.Clone();
            messageCopy.Text = dataEncrypt.EncryptedText;
            messageCopy.IV = dataEncrypt.IV;
            messageCopy.HMAC = dataEncrypt.Hmac;
        
            var success = await _messageRepository.UpdateMessageAsync(messageCopy);
        
            if (!success)
                return CreateErrorResponse<string, MessageData>("Сообщение не изменено", ResponseType.MessageNotModified, 400, "Bad Request");
            
            _ = _manager.SendMessageToUserAsync(updateMessage, chat.Persons);
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение успешно изменено",
                Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = updateMessage
            };
        }

        if (chat.Type == ChatType.SecretPrivate)
        {
            if (messageData.Keys is null || messageData.Signature is null)
                return CreateErrorResponse<string, MessageData>("У сообщения отсутствуют обязательные поля", ResponseType.MissingRequiredFields, 400, "Bad Request");
            
            message.Keys = messageData.Keys;
            message.Signature = messageData.Signature;
            
            var success = await _messageRepository.UpdateMessageAsync(message);
        
            if (!success)
                return CreateErrorResponse<string, MessageData>("Сообщение не изменено", ResponseType.MessageNotModified, 400, "Bad Request");
            
            _ = _manager.SendMessageToUserAsync(message, chat.Persons);
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение успешно изменено",
                Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = message
            };
        }
        
        return CreateErrorResponse<string, MessageData>("Неизвестный тип чата", ResponseType.UnknownChatType, 400, "Bad Request");
    }

    public async Task<BaseResponse<string, string>> ReadTheMessage(string accessToken, string chatId, string messageId)
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
        
        if (message.OwnerId == dataToken.PersonId)
            return CreateErrorResponse<string, string>("Вы не можете это сделать для своего сообщения", ResponseType.MessageView, 403, "Forbidden");
        
        if (message.Views.All(v => v.PersonId != dataToken.PersonId))
        {
            ReadMessage readMessage = new ReadMessage
            {
                PersonId = dataToken.PersonId,
                TimeStamp = DateTime.UtcNow
            };
            message.Views.Add(readMessage);
            await _messageRepository.UpdateMessageAsync(message);
            await _manager.SendMessageViewMessage(chatId, messageId, chat.Persons.FirstOrDefault(p => p.PersonId == message.OwnerId)!);
        }
        
        return new BaseResponse<string, string>
        {
            Message = "Успешно",
            Type = ResponseType.Ok,
            Status = 200,
            Successfully = true,
            Data = "Отмечено как прочтенное"
        };
    }

    public async Task<BaseResponse<string, List<PersonReadMessage>>> GetTheReadMessage(string accessToken, string chatId, string messageId)
    {
        var dataToken = _jwtTokenService.GetJwtTokenData(accessToken);
        if (!await _jwtTokenService.ValidateJwtAccessToken(dataToken))
            return CreateErrorResponse<string, List<PersonReadMessage>>("Не удалось проверить корректность jwt токена", ResponseType.JwtTokenVerificationFailed, 403, "Forbidden");

        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null || chat.HiddenForUsers.Contains(dataToken.PersonId))
            return CreateErrorResponse<string, List<PersonReadMessage>>("Данный чат не найден", ResponseType.ChatNotFound, 404, "Not Found");

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return CreateErrorResponse<string, List<PersonReadMessage>>("Вы не состоите в этом чате", ResponseType.UserNotInChat, 403, "Forbidden");
        
        var message = await _messageRepository.GetMessageByIdAsync(chatId, messageId, dataToken.PersonId);
        
        if (message is null)
            return CreateErrorResponse<string, List<PersonReadMessage>>("Сообщение не найдено!", ResponseType.MessageNotFound, 404, "Not Found");
        
        if (message.OwnerId != dataToken.PersonId)
            return CreateErrorResponse<string, List<PersonReadMessage>>("Вы не можете это сделать для чужого сообщения", ResponseType.MessageView, 403, "Forbidden");

        List<PersonReadMessage> readMessages = new List<PersonReadMessage>();
        foreach (var personRead in message.Views)
        {
            var result = await _profileRepository.GetSummaryPersonDataAsync(personRead.PersonId);
            
            if (result is null)
                continue;
            
            PersonReadMessage read = new PersonReadMessage
            {
                Name = result.Name,
                Surname = result.Surname,
                ProfileImageUrl = result.Image?.Url,
                TimeStamp = personRead.TimeStamp,
            };
            readMessages.Add(read);
        }
        
        return new BaseResponse<string, List<PersonReadMessage>>
        {
            Message = "Просмотры сообщения",
            Type = ResponseType.Ok,
            Status = 200,
            Successfully = true,
            Data = readMessages
        };
    }

    private (string EncryptedText, string IV, string Hmac) EncryptAndSign(string plainText)
    {
        // Генерация IV
        var iv = RandomNumberGenerator.GetBytes(16);

        // Шифрование
        IMessageEncryptionService encryption = new MessageEncryptionService(_aesKey, iv);
        var encryptedText = encryption.Encrypt(plainText);
        var ivBase64 = Convert.ToBase64String(iv);
        
        // Вычисляем HMAC от зашифрованного текста
        IHmacService hmacService = new HmacService(_hmacKey);
        var hmac = hmacService.ComputeHmac(encryptedText);

        return (encryptedText, ivBase64, hmac);
    }

    private MessageData? DecryptAndVerify(MessageData messageData)
    {
        IHmacService hmacService = new HmacService(_hmacKey);
        var result = hmacService.VerifyHmac(messageData.Text, messageData.HMAC!);

        if (!result)
        {
            _logger.LogWarning("Подпись hmac не совпадает для сообщения: {messageId}", messageData.Id);
            return null;
        }
        
        var iv = Convert.FromBase64String(messageData.IV!);
        IMessageEncryptionService encryption = new MessageEncryptionService(_aesKey, iv);
        var decryptedText = encryption.Decrypt(messageData.Text);
        messageData.Text = decryptedText;
        return messageData;
    }
    
    private List<MessageData> DecryptAndVerifyMany(List<MessageData> messages)
    {
        return messages
            .Select(DecryptAndVerify)
            .Where(m => m != null)
            .ToList()!;
    }
    
    /// <summary>
    /// Создает объект сообщения
    /// </summary>
    /// <param name="message">Данные сообщения</param>
    /// <param name="ownerPersonId">Автор сообщения</param>
    /// <returns></returns>
    private MessageData CreateMessageAsync(Message message, string ownerPersonId)
    {
        MessageData messageData = new MessageData
        {
            MessageId = Guid.NewGuid().ToString(),
            ChatId = message.ChatId,
            Text = message.Text,
            Created = DateTime.UtcNow,
            MessageType = MessageStatus.Send,
            OwnerId = ownerPersonId,
            Signature = message.Signature,
            Keys = message.Keys
        };
        
        return messageData;
    }

    /// <summary>
    /// Шифрует и сохраняет сообщение в БД
    /// </summary>
    /// <param name="messageData"></param>
    /// <param name="cancellationToken"></param>
    private async Task SaveMessageAsync(MessageData messageData, CancellationToken cancellationToken)
    {
        (string EncryptedText, string IV, string Hmac) dataEncrypt = EncryptAndSign(messageData.Text);
        messageData.Text = dataEncrypt.EncryptedText;
        messageData.IV = dataEncrypt.IV;
        messageData.HMAC = dataEncrypt.Hmac;
        await _messageRepository.SaveMessageAsync(messageData, cancellationToken);
    }
}