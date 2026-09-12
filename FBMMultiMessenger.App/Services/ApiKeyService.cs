using FBMMultiMessenger.Contracts.Contracts.ApiKey;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    internal class ApiKeyService : IApiKeyService
    {
        private readonly IBaseService _baseService;

        public ApiKeyService(IBaseService baseService)
        {
            this._baseService=baseService;
        }

        public async Task<BaseResponse<GetMyApiKeyHttpResponse>> GetMyApiKeyAsync()
        {
            var apiRequest = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = "apikey/me",
                Data = null
            };

            return await _baseService.SendAsync<object, BaseResponse<GetMyApiKeyHttpResponse>>(apiRequest);
        }

        public async Task<BaseResponse<UpsertApiKeyHttpResponse>> GenerateApiKeyAsync()
        {
            var apiRequest = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.POST,
                Url = "apikey",
                Data = null
            };

            return await _baseService.SendAsync<object, BaseResponse<UpsertApiKeyHttpResponse>>(apiRequest);
        }

        public async Task<BaseResponse<UpsertApiKeyHttpResponse>> RegenerateApiKeyAsync()
        {
            var apiRequest = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.POST,
                Url = "apikey/regenerate",
                Data = null
            };

            return await _baseService.SendAsync<object, BaseResponse<UpsertApiKeyHttpResponse>>(apiRequest);
        }
    }
}
