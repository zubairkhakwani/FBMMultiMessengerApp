using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Components.Pages.Auth
{
    public partial class ForgotPassword
    {
        public ForgotPasswordHttpRequest ForgotPasswordModel { get; set; } = new ForgotPasswordHttpRequest();
        public VerifyOtp VerifyOtpModel { get; set; } = new VerifyOtp();
        public ResetPasswordHttpRequest ResetPasswordModel { get; set; } = new ResetPasswordHttpRequest();


        [Inject]
        public IAuthService AuthService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        public bool IsEmailSend { get; set; }
        public bool IsEmailSending { get; set; }


        public bool IsOtpVerified { get; set; }
        public bool IsOtpVerifying { get; set; }

        public bool IsRessetingNewPassword { get; set; }

        public string OtpErrorMessage { get; set; } = string.Empty;
        public string OtpSuccessMessage { get; set; } = string.Empty;

        public string EmailSentTo { get; set; } = string.Empty;
        private string Password = string.Empty;



        public async Task OnValidEmailSubmit()
        {
            IsEmailSending = true;
            OtpSuccessMessage = string.Empty;

            var response = await AuthService.ForgotPasswordAsync(ForgotPasswordModel);

            IsEmailSending = false;

            if (response.IsSuccess)
            {
                Snackbar.Add(response.Message, Severity.Success);
                IsEmailSend = true;
                EmailSentTo = ForgotPasswordModel.Email;
                OtpSuccessMessage = response.Message;
                return;
            }

            Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong when sending email, please try later" : response.Message, Severity.Error);
        }


        public async Task OnValidOtpSubmit()
        {
            OtpErrorMessage = string.Empty;
            OtpSuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(VerifyOtpModel.Digit1) || string.IsNullOrWhiteSpace(VerifyOtpModel.Digit2) ||string.IsNullOrWhiteSpace(VerifyOtpModel.Digit3) ||string.IsNullOrWhiteSpace(VerifyOtpModel.Digit4)  || string.IsNullOrWhiteSpace(VerifyOtpModel.Digit5) || string.IsNullOrWhiteSpace(VerifyOtpModel.Digit6))
            {
                Snackbar.Add("Please enter verification code to continue", Severity.Success);
                return;
            }

            IsOtpVerifying = true;

            var otp = $"{VerifyOtpModel.Digit1}{VerifyOtpModel.Digit2}{VerifyOtpModel.Digit3}{VerifyOtpModel.Digit4}{VerifyOtpModel.Digit5}{VerifyOtpModel.Digit6}";

            var response = await AuthService.VerifyOtpAsync(otp);

            IsOtpVerifying = false;

            if (response.IsSuccess)
            {
                Snackbar.Add(response.Message, Severity.Success);
                IsOtpVerified  = true;

                ResetPasswordModel.Otp = otp; //we need this in order to get the current user that is trying to change password
                return;
            }

            OtpErrorMessage = response.Message;
            Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong, please try later" : response.Message, Severity.Error);
        }

        public async Task HandleResetPassword()
        {
            IsRessetingNewPassword = true;

            var response = await AuthService.ResetPasswordAsync(ResetPasswordModel);

            IsRessetingNewPassword = false;

            if (response.IsSuccess)
            {
                Snackbar.Add(response.Message, Severity.Success);
                NavigationManager.NavigateTo("/Login");
                return;
            }

            Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong, please try later" : response.Message, Severity.Error);
        }

        public async Task HandleResendOtp()
        {
            IsOtpVerifying = true;
            ForgotPasswordModel.IsResendRequest = true;
            OtpSuccessMessage = string.Empty;
            OtpErrorMessage = string.Empty;
            VerifyOtpModel = new VerifyOtp();
            await OnValidEmailSubmit();
            IsOtpVerifying = false;

        }
        private void IsStrongPassword(ChangeEventArgs input)
        {
            Password = input?.Value?.ToString() ?? "";
        }
    }

    public class VerifyOtp
    {
        [Required]
        public string Digit1 { get; set; } = string.Empty;

        [Required]
        public string Digit2 { get; set; } = string.Empty;

        [Required]
        public string Digit3 { get; set; } = string.Empty;

        [Required]
        public string Digit4 { get; set; } = string.Empty;

        [Required]
        public string Digit5 { get; set; } = string.Empty;

        [Required]
        public string Digit6 { get; set; } = string.Empty;
    }
}
