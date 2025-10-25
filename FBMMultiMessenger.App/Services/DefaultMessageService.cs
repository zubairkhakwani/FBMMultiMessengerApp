using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    internal class DefaultMessageService : IDefaultMessageService
    {
        private readonly IBaseService _baseService;

        public DefaultMessageService(IBaseService baseService)
        {
            this._baseService=baseService;
        }

        public async Task<BaseResponse<UpsertDefaultMessageHttpResponse>> UpsertDefaultMessageAsync(UpsertDefaultMessageHttpRequest httpRequest, int? defaultMessageId)
        {
            var request = new ApiRequest<UpsertDefaultMessageHttpRequest>()
            {
                ApiType =  defaultMessageId is null ? SD.ApiType.POST : SD.ApiType.PUT,
                Url =  defaultMessageId is null ? "api/defaultmessage" : $"api/defaultmessage/{defaultMessageId}",
                Data = httpRequest
            };
            return await _baseService.SendAsync<UpsertDefaultMessageHttpRequest, BaseResponse<UpsertDefaultMessageHttpResponse>>(request);
        }

        public async Task<BaseResponse<GetMyDefaultMessagesHttpResponse>> GetMyDefaultMessagesAsync()
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url ="api/defaultmessage/me",
                Data = null
            };
            return await _baseService.SendAsync<object, BaseResponse<GetMyDefaultMessagesHttpResponse>>(request);
        }
    }
}

