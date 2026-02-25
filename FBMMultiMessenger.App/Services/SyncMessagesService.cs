using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using System.Globalization;
using System.Web;

namespace FBMMultiMessenger.Services
{
    public class SyncMessagesService : ISyncMessagesService
    {
        private readonly IBaseService baseService;

        public SyncMessagesService(IBaseService baseService)
        {
            this.baseService = baseService;
        }

        public async Task<BaseResponse<GetUnSyncedMessagesHttpResponse>> GetUnSyncedMessages(DateTimeOffset? lastSyncedAt, CancellationToken cancellationToken = default)
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = $"chat/get-unsynced-messages?lastSyncedAt={HttpUtility.UrlEncode(lastSyncedAt?.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture))}",
                Data = null
            };

            return await baseService.SendAsync<object, BaseResponse<GetUnSyncedMessagesHttpResponse>>(request, cancellationToken: cancellationToken);
        }
    }
}
