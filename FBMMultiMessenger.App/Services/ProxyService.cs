using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Proxy;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    internal class ProxyService : IProxyService
    {
        private readonly IBaseService _baseService;

        public ProxyService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<BaseResponse<PageableResponse<GetMyProxiesHttpResponse>>> GetMyProxiesAsync(GetMyProxiesHttpRequest httpRequest)
        {
            var apiRequest = new ApiRequest<GetMyProxiesHttpRequest>()
            {
                ApiType = Utility.SD.ApiType.GET,
                Url = "proxy/me",
                Data = httpRequest
            };

            return await _baseService.SendAsync<GetMyProxiesHttpRequest, BaseResponse<PageableResponse<GetMyProxiesHttpResponse>>>(apiRequest);
        }

        public async Task<BaseResponse<UpsertProxyHttpResponse>> UpsertProxyAsync(UpsertProxyHttpRequest httpRequest, int? proxyId)
        {
            var url = proxyId is not null ? $"proxy/{proxyId}" : "proxy";

            var request = new ApiRequest<UpsertProxyHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url = url,
                Data = httpRequest
            };

            return await _baseService.SendAsync<UpsertProxyHttpRequest, BaseResponse<UpsertProxyHttpResponse>>(request);
        }
    }
}
