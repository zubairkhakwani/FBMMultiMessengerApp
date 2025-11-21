using FBMMultiMessenger.Contracts.Contracts.Payment;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IBaseService _baseService;

        public PaymentService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<BaseResponse<AddPaymentProofHttpResponse>> SubmitProof(AddPaymentProofHttpRequest httpRequest)
        {
            var apiRequest = new ApiRequest<AddPaymentProofHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url = "Payment/proof",
                ContentType = SD.ContentType.MultipartFormData,
                Data = httpRequest
            };

            return await _baseService.SendAsync<AddPaymentProofHttpRequest, BaseResponse<AddPaymentProofHttpResponse>>(apiRequest);
        }
        public async Task<BaseResponse<GetMyVerificationStatusHttpResponse>> GetMyStatus()
        {
            var apiRequest = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = "payment/me/status",
                Data  = null
            };

            return await _baseService.SendAsync<object, BaseResponse<GetMyVerificationStatusHttpResponse>>(apiRequest);
        }
    }
}
