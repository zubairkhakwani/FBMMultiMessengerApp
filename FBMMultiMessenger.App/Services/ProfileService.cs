using FBMMultiMessenger.Contracts.Contracts.Profile;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IBaseService _baseService;

        public ProfileService(IBaseService baseService)
        {
            this._baseService=baseService;
        }


        public async Task<BaseResponse<object>> ChangePasswordAsync(ChangePasswordHttpRequest httpRequest)
        {
            var request = new ApiRequest<ChangePasswordHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url = "profile/me/changepassword",
                Data = httpRequest
            };
            return await _baseService.SendAsync<ChangePasswordHttpRequest, BaseResponse<object>>(request);
        }

        public async Task<BaseResponse<object>> EditProfileAsync(EditProfileHttpRequest httpRequest)
        {
            var request = new ApiRequest<EditProfileHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url = "profile/me/edit",
                Data = httpRequest

            };

            return await _baseService.SendAsync<EditProfileHttpRequest, BaseResponse<object>>(request);
        }

        public async Task<BaseResponse<GetMyProfleHttpResponse>> GetMyProfileAsync()
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = "profile/me",
                Data = null
            };

            return await _baseService.SendAsync<object, BaseResponse<GetMyProfleHttpResponse>>(request);
        }
    }
}
