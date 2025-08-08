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
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IWebSocketConnectionManager _manager;
    
    private readonly byte[] _aesKey;   // 32 байта
    private readonly byte[] _hmacKey;  // 32 байта

    public MessageService(IConfiguration configuration, ILogger<MessageService> logger, IMessageRepository messageRepository, IJwtTokenService jwtTokenService, IWebSocketConnectionManager manager)
    {
        _logger = logger;
        _messageRepository = messageRepository;
        _jwtTokenService = jwtTokenService;
        _manager = manager;
        
        _aesKey = Convert.FromBase64String(configuration["MessageEncryption:Key"]);
        _hmacKey = Convert.FromBase64String(configuration["MessageEncryption:Hmac"]);
    }
    
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
        ChatDocument? chat = await _messageRepository.GetChatAsync(message.ChatId);
        
        if (chat is null)
            return new BaseResponse<string, SendMessage>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };
        
        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, SendMessage>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };

        if (chat.Type != ChatType.Private)
        {
            var personRole = chat.Persons.FirstOrDefault(p => p.PersonId == dataToken.PersonId);
        
            if (personRole is null || personRole.Role == Roles.User)
                return new BaseResponse<string, SendMessage>
                {
                    Message = "Вам запрещено отправлять сообщения в этот чат",
                    Type = ResponseType.ChatNotFound, Errors = "Forbidden", Status = 403,
                    Successfully = false, Data = null
                };
        }

        MessageData messageData = CreateMessageAsync(message, dataToken.PersonId);
        
        var copyMessage = (MessageData)messageData.Clone();
        await SaveMessageAsync(copyMessage, cancellationToken);
        
        _ = SendMessageUsersAsync(chat.Persons, messageData);
        MetricsRegistry.MessagesSentCounter.Inc();

        return new BaseResponse<string, SendMessage>
        {
            Message = "Сообщение успешно отправлено",
            Type = ResponseType.Ok, Status = 200,
            Successfully = true, Errors = null, Data = new SendMessage{ Message = messageData}
        };
    }
    
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
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessagesPagination>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, MessagesPagination>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };

        (List<MessageData> messages, long totalCount) = await _messageRepository.GetMessagesByChatIdAsync(chatId, limit, offset);
        List<MessageData> decryptedMessages = DecryptAndVerifyMany(messages);

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
                Messages = decryptedMessages
            }
        };
    }
    
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
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
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

        MessageData? lastMessage = DecryptAndVerify(message);
        return new BaseResponse<string, MessageData>
        {
            Message = "Последнее сообщение чата",
            Type = ResponseType.Ok, Status = 200, Successfully = true,
            Errors = null, Data = lastMessage
        };
    }
    
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
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, MessageData>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        
        if (chat.Type != ChatType.Private && chat.Type != ChatType.Bot && chat.Persons.FirstOrDefault(p => p.PersonId != dataToken.PersonId) is { Role: Roles.User })
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
        message.Updated = DateTime.UtcNow;
        var success = await _messageRepository.UpdateMessageAsync(message);
        
        if (!success)
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение не удалено",
                Type = ResponseType.MessageNotModified, Errors = "BadRequest", Status = 400,
                Successfully = false, Data = null
            };

        _ = SendMessageUsersAsync(chat.Persons, message);
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
        {
            return new BaseResponse<string, MessageData>
            {
                Message = "Не удалось проверить корректность jwt токена",
                Type = ResponseType.JwtTokenVerificationFailed, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        }
        ChatDocument? chat = await _messageRepository.GetChatAsync(chatId);
        
        if (chat is null)
            return new BaseResponse<string, MessageData>
            {
                Message = "Данный чат не найден",
                Type = ResponseType.ChatNotFound, Errors = "Not Found", Status = 404,
                Successfully = false, Data = null
            };

        if (chat.Persons.All(p => p.PersonId != dataToken.PersonId))
            return new BaseResponse<string, MessageData>
            {
                Message = "Вы не состоите в этом чате",
                Type = ResponseType.UserNotInChat, Errors = "Forbidden", Status = 403,
                Successfully = false, Data = null
            };
        
        if (chat.Type != ChatType.Private && chat.Type != ChatType.Bot && chat.Persons.FirstOrDefault(p => p.PersonId != dataToken.PersonId) is { Role: Roles.User })
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

        MessageData? updateMessage = DecryptAndVerify(message); // открытый текст
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
            return new BaseResponse<string, MessageData>
            {
                Message = "Сообщение не изменено",
                Type = ResponseType.MessageNotModified, Errors = "BadRequest", Status = 400,
                Successfully = false, Data = null
            };

        _ = SendMessageUsersAsync(chat.Persons, updateMessage);
        return new BaseResponse<string, MessageData>
        {
            Message = "Сообщение успешно изменено",
            Type = ResponseType.Ok, Status = 200, Successfully = true, Errors = null, Data = updateMessage
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
        var result = hmacService.VerifyHmac(messageData.Text, messageData.HMAC);

        if (!result)
        {
            _logger.LogWarning("Подпись hmac не совпадает для сообщения: {messageId}", messageData.Id);
            return null;
        }
        
        var IV = Convert.FromBase64String(messageData.IV);
        IMessageEncryptionService encryption = new MessageEncryptionService(_aesKey, IV);
        var decryptedText = encryption.Decrypt(messageData.Text);
        messageData.Text = decryptedText;
        return messageData;
    }
    
    public List<MessageData> DecryptAndVerifyMany(List<MessageData> messages)
    {
        return messages
            .Select(DecryptAndVerify)
            .Where(m => m != null)
            .ToList()!;
    }
    
    /// <summary>
    /// Отправляет сообщение пользователям по WebSocket
    /// </summary>
    /// <param name="users"></param>
    /// <param name="message"></param>
    private async Task SendMessageUsersAsync(List<ChatUser> users, MessageData message)
    {
        foreach (var user in users)
            await _manager.SendMessageToUserAsync(user.PersonId, message);
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
            OwnerId = ownerPersonId
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