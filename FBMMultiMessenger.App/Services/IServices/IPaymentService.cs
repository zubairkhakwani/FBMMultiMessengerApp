using FBMMultiMessenger.Contracts.Contracts.Payment;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IPaymentService
    {
        Task<BaseResponse<AddPaymentProofHttpResponse>> SubmitProof(AddPaymentProofHttpRequest httpRequest);
        Task<BaseResponse<GetMyVerificationStatusHttpResponse>> GetMyStatus();
    }
}
