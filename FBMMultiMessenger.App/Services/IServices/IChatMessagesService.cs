using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IChatMessagesService
    {
        Task<BaseResponse<List<GeChatMessagesHttpResponse>>> GetChatMessages(int chatId, CancellationToken cancellationToken = default);
    }
}
