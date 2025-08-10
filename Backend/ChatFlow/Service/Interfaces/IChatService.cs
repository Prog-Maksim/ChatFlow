using ChatFlow.Models.Response;

namespace ChatFlow.Service.Interfaces;

public interface IChatService
{
    public Task<BaseResponse<string, CreateChat>> CreatePrivateChat(string accessToken, string otherPersonId);
    public Task<BaseResponse<string, CreateChat>> CreateSecretPrivateChat(string accessToken, string otherPersonId);

    public Task<BaseResponse<string, Chats>> GetChats(string accessToken);

    public Task<BaseResponse<string, ChatInfo>> GetChatInfo(string accessToken, string chatId);
    
    public Task<BaseResponse<string, string>> DeleteAllMessages(string accessToken, string chatId);
}