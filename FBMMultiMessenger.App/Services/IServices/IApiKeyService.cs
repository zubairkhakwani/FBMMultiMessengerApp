using FBMMultiMessenger.Contracts.Contracts.ApiKey;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IApiKeyService
    {
        Task<BaseResponse<GetMyApiKeyHttpResponse>> GetMyApiKeyAsync();
        Task<BaseResponse<UpsertApiKeyHttpResponse>> GenerateApiKeyAsync();
        Task<BaseResponse<UpsertApiKeyHttpResponse>> RegenerateApiKeyAsync();
    }
}
