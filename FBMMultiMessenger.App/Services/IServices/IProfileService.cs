using FBMMultiMessenger.Contracts.Contracts.Profile;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Models;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IProfileService
    {
        Task<BaseResponse<object>> EditProfileAsync(EditProfileHttpRequest httpRequest);
        Task<BaseResponse<object>> ChangePasswordAsync(ChangePasswordHttpRequest httpRequest);
        Task<BaseResponse<GetMyProfleHttpResponse>> GetMyProfileAsync();
    }
}
