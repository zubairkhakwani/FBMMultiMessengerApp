using FBMMultiMessenger.Contracts.Contracts.Extension;

namespace FBMMultiMessenger.Services.IServices
{
    public interface ILocalServerService
    {
        Task<T> Notify<T>(NotifyLocalServerHttpRequest httpRequest) where T : class, new();
    }
}
