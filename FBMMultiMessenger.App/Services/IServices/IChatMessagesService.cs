using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IChatMessagesService
    {
        Task<BaseResponse<List<GeChatMessagesHttpResponse>>> GetChatMessages(string fbChatId, CancellationToken cancellationToken = default);
    }
}
