using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface ISyncMessagesService
    {
        public Task<BaseResponse<GetUnSyncedMessagesHttpResponse>> GetUnSyncedMessages(DateTimeOffset? lastSyncedAt, CancellationToken cancellationToken = default);
    }
}
