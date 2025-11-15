using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Components.Pages.Shared
{
    public partial class OtpValidator
    {
        public VerifyOtp VerifyOtpModel { get; set; } = new VerifyOtp();
        public ResetPasswordHttpRequest ResetPasswordModel { get; set; } = new ResetPasswordHttpRequest();
        [Inject]
        public IAuthService AuthService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public ICurrentUserService CurrentUserService { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }


        public bool IsOtpVerified { get; set; }
        public bool IsOtpVerifying { get; set; }
        public bool IsRessetingNewPassword { get; set; }

        [Parameter, SupplyParameterFromQuery]
        public string OtpErrorMessage { get; set; } = string.Empty;

        [Parameter, SupplyParameterFromQuery]
        public string OtpSuccessMessage { get; set; } = string.Empty;

        [Parameter, SupplyParameterFromQuery]
        public string EmailSentTo { get; set; } = string.Empty;

        [Parameter, SupplyParameterFromQuery]
        public string? TotalSteps { get; set; }

        [Parameter, SupplyParameterFromQuery]
        public string? CurrentStep { get; set; }

        [Parameter, SupplyParameterFromQuery]
        public string? ReturnUrl { get; set; }

        [Parameter, SupplyParameterFromQuery]
        public string? ReturnTo { get; set; }

        [Parameter, SupplyParameterFromQuery]
        public string IsEmailVerification { get; set; } = string.Empty;

        private string Password = string.Empty;

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

            var isParsed = bool.TryParse(IsEmailVerification, out bool isEmailVerification);

            var response = await AuthService.VerifyOtpAsync(otp, isEmailVerification: isEmailVerification);

            IsOtpVerifying = false;

            if (response.IsSuccess)
            {
                Snackbar.Add(response.Message, Severity.Success);
                IsOtpVerified  = true;
                ResetPasswordModel.Otp = otp; //we need this in order to check who is making the request
                if (isEmailVerification)
                {
                    Navigation.NavigateTo("/Account");
                }

                return;
            }

            OtpErrorMessage = response.Message;
            Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong, please try later" : response.Message, Severity.Error);
        }

        public async Task HandleResendOtp()
        {
            IsOtpVerifying = true;

            OtpSuccessMessage = string.Empty;

            OtpErrorMessage = string.Empty;

            VerifyOtpModel.Digit1 = VerifyOtpModel.Digit2 =VerifyOtpModel.Digit3 =VerifyOtpModel.Digit4 = VerifyOtpModel.Digit5 =VerifyOtpModel.Digit6 ="";

            var isParsed = bool.TryParse(IsEmailVerification, out bool isEmailVerification);

            var response = await AuthService.ResendOtpAsync(EmailSentTo, isEmailVerification: isEmailVerification);

            IsOtpVerifying = false;

            if (response.IsSuccess)
            {
                Snackbar.Add(response.Message, Severity.Success);
                OtpSuccessMessage = response.Message;
                return;
            }

            Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong when re-sending email, please try later" : response.Message, Severity.Error);
        }


        public async Task HandleResetPassword()
        {
            IsRessetingNewPassword = true;

            var response = await AuthService.ResetPasswordAsync(ResetPasswordModel);

            IsRessetingNewPassword = false;

            if (response.IsSuccess)
            {
                Snackbar.Add(response.Message, Severity.Success);
                Navigation.NavigateTo(ReturnUrl);
                return;
            }

            Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong, please try later" : response.Message, Severity.Error);
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
