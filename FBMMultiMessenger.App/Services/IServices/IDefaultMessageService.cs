using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IDefaultMessageService
    {
        Task<BaseResponse<UpsertDefaultMessageHttpResponse>> UpsertDefaultMessageAsync(UpsertDefaultMessageHttpRequest request, int? defaultMessageId);
        Task<BaseResponse<GetMyDefaultMessagesHttpResponse>> GetMyDefaultMessagesAsync();

    }
}
