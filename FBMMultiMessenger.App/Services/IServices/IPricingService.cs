using FBMMultiMessenger.Contracts.Contracts.Pricing;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IPricingService
    {
        Task<BaseResponse<GetAllPricingHttpResponse>> GetAll();
    }
}
