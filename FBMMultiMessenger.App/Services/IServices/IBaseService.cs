using FBMMultiMessenger.Request;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IBaseService
    {
        Task<TResponse> SendAsync<TRequest, TResponse>(ApiRequest<TRequest> apiRequest, bool withBearer = true, CancellationToken cancellationToken = default) where TResponse : class, new()
                                                                                where TRequest : class;
    }
}
