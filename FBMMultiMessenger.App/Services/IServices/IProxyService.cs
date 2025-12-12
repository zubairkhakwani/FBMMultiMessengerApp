using FBMMultiMessenger.Contracts.Contracts.Proxy;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IProxyService
    {
        Task<BaseResponse<PageableResponse<GetMyProxiesHttpResponse>>> GetMyProxiesAsync(GetMyProxiesHttpRequest httpRequest);
        Task<BaseResponse<UpsertProxyHttpResponse>> UpsertProxyAsync(UpsertProxyHttpRequest httpRequest, int? proxyId);
    }
}
