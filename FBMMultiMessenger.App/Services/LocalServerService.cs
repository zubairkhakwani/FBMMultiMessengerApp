using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using static FBMMultiMessenger.Utility.SD;

namespace FBMMultiMessenger.Services
{
    public class LocalServerService : ILocalServerService
    {
        private readonly IBaseService _baseService;

        public LocalServerService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<T> Notify<T>(NotifyLocalServerHttpRequest httpRequest) where T : class, new()
        {
            var request = new ApiRequest<NotifyLocalServerHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url ="localserver/notify",
                Data = httpRequest,
                ContentType = ContentType.MultipartFormData
            };

            return await _baseService.SendAsync<NotifyLocalServerHttpRequest, T>(request);

        }
    }
}
