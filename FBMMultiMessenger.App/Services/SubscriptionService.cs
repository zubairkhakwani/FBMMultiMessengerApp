using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;

namespace FBMMultiMessenger.Services
{
    internal class SubscriptionService : ISubscriptionSerivce
    {
        private readonly IBaseService _baseService;

        public SubscriptionService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<T> GetMySubscription<T>() where T : class, new()
        {
            var apiRequest = new ApiRequest<object>()
            {
                ApiType = Utility.SD.ApiType.GET,
                Url = "subscription/me"

            };
            return await _baseService.SendAsync<object, T>(apiRequest);
        }
    }
}
