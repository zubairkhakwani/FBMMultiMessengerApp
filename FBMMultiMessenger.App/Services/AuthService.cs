using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using OneSignalSDK.DotNet;

namespace FBMMultiMessenger.Services
{
    internal class AuthService : IAuthService
    {
        private readonly IBaseService _baseService;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ITokenProvider TokenProvider;

        public AuthService(IBaseService baseService, AuthenticationStateProvider authState, ITokenProvider tokenProvider)
        {
            this._baseService=baseService;
            this._authenticationStateProvider = authState;
            this.TokenProvider =tokenProvider;

        }

        public async Task<BaseResponse<object>> ForgotPasswordAsync(ForgotPasswordHttpRequest httpRequest)
        {
            var apiRequest = new ApiRequest<ForgotPasswordHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url ="auth/forgot-password",
                Data = httpRequest
            };

            return await _baseService.SendAsync<ForgotPasswordHttpRequest, BaseResponse<object>>(apiRequest);
        }

        public async Task<T> LoginAsync<T>(LoginHttpRequest httpRequest) where T : class, new()
        {
            var apiRequest = new ApiRequest<LoginHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url ="auth/login",
                Data = httpRequest
            };

            return await _baseService.SendAsync<LoginHttpRequest, T>(apiRequest);
        }

        public async Task Logout()
        {
            await TokenProvider.RemoveTokenAsync();
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();

            if (PlatformHelper.IsMobilePlatform)
            {
                OneSignal.Logout();
            }
        }

        public async Task<T> RegisterAsync<T>(RegisterHttpRequest httpRequest) where T : class, new()
        {
            var apiRequest = new ApiRequest<RegisterHttpRequest>()
            {
                ApiType  = SD.ApiType.POST,
                Url = "auth/register",
                Data  = httpRequest
            };

            return await _baseService.SendAsync<RegisterHttpRequest, T>(apiRequest);
        }

        public async Task<BaseResponse<object>> ResendOtpAsync(string email, bool isEmailVerification = false)
        {
            var apiRequest = new ApiRequest<string>()
            {
                ApiType  = SD.ApiType.POST,
                Url = $"auth/resend-otp?isEmailVerification={isEmailVerification}",
                Data  = email
            };

            return await _baseService.SendAsync<string, BaseResponse<object>>(apiRequest);
        }

        public async Task<BaseResponse<object>> ResetPasswordAsync(ResetPasswordHttpRequest httpRequest)
        {
            var apiRequest = new ApiRequest<ResetPasswordHttpRequest>()
            {
                ApiType  = SD.ApiType.POST,
                Url = "auth/reset-password",
                Data  = httpRequest
            };

            return await _baseService.SendAsync<ResetPasswordHttpRequest, BaseResponse<object>>(apiRequest);
        }

        public async Task<BaseResponse<object>> VerifyOtpAsync(string otp, bool isEmailVerification = false)
        {
            var apiRequest = new ApiRequest<string>()
            {
                ApiType  = SD.ApiType.POST,
                Url = $"auth/verify-otp?isEmailVerification={isEmailVerification}",
                Data  = otp
            };

            return await _baseService.SendAsync<string, BaseResponse<object>>(apiRequest);
        }
    }
}
