using FBMMultiMessenger.Contracts.Contracts.Profile;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    internal interface IProfileService
    {
        Task<BaseResponse<object>> EditProfileAsync(EditProfileHttpRequest httpRequest);
        Task<BaseResponse<object>> ChangePasswordAsync(ChangePasswordHttpRequest httpRequest);
        Task<BaseResponse<GetMyProfileHttpResponse>> GetMyProfileAsync();
    }
}
