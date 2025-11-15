using FBMMultiMessenger.Request;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IBaseService
    {
        Task<TResponse> SendAsync<TRequest, TResponse>(ApiRequest<TRequest> apiRequest, bool withBearer = true) where TResponse : class, new()
                                                                                where TRequest : class;
    }
}
