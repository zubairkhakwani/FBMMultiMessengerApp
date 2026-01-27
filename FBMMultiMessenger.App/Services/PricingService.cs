using FBMMultiMessenger.Contracts.Contracts.Pricing;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    public class PricingService : IPricingService
    {
        private readonly IBaseService _baseService;

        public PricingService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<BaseResponse<GetAllPricingHttpResponse>> GetAll()
        {

            var apiRequest = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = "pricing",
                Data = null
            };

            return await _baseService.SendAsync<object, BaseResponse<GetAllPricingHttpResponse>>(apiRequest);
        }
    }
}
