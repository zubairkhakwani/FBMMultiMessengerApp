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



        [Inject]
        public IAuthService AuthService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        public bool IsEmailSend { get; set; }
        public bool IsEmailSending { get; set; }

        public string OtpErrorMessage { get; set; } = string.Empty;
        public string OtpSuccessMessage { get; set; } = string.Empty;

        public string EmailSentTo { get; set; } = string.Empty;




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
