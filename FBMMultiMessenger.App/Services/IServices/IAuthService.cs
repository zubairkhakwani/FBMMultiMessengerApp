using FBMMultiMessenger.Components.Pages.Auth;
using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IAuthService
    {
        Task<T> LoginAsync<T>(LoginHttpRequest httpRequest) where T : class, new();
        Task<T> RegisterAsync<T>(RegisterHttpRequest httpRequest) where T : class, new();
        Task<BaseResponse<object>> ForgotPasswordAsync(ForgotPasswordHttpRequest httpRequest);

        Task<BaseResponse<object>> VerifyOtpAsync(string otp, bool isEmailVerification = false);
        Task<BaseResponse<object>> ResendOtpAsync(string email, bool isEmailVerification = false);

        Task<BaseResponse<object>> ResetPasswordAsync(ResetPasswordHttpRequest httpRequest);

        Task Logout();

    }
}
