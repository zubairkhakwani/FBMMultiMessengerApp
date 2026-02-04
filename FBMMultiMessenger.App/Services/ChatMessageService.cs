using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    internal class ChatMessageService : IChatMessagesService
    {
        private readonly IBaseService _baseService;

        public ChatMessageService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<BaseResponse<List<GeChatMessagesHttpResponse>>> GetChatMessages(int chatId, CancellationToken cancellationToken = default)
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = $"chat/{chatId}/chatmessages",
                Data = null
            };

            return await _baseService.SendAsync<object, BaseResponse<List<GeChatMessagesHttpResponse>>>(request, cancellationToken: cancellationToken);
        }
    }
}
