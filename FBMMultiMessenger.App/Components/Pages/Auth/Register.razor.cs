using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Auth
{
    public partial class Register
    {
        //[SupplyParameterFromForm]
        public RegisterHttpRequest RequestModel { get; set; } = new();

        [Inject]
        public IAuthService AuthService { get; set; }

        [Inject]
        public NavigationManager navManager { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        public string? ResponseError { get; set; }

        [Inject]
        public ITokenProvider TokenProvider { get; set; }


        private bool ShowLoader = false;

        private string Password = string.Empty;

        private bool ShowPassword = false;
        private bool ShowConfirmPassword = false;

        private string PasswordType = "password";
        private string PasswordConfirmType = "password";
        private string ConfirmPasswordErrorMessage = string.Empty;
        private bool DoesPasswordMatch = false;


        public async Task OnValidSubmit()
        {
            if (string.IsNullOrWhiteSpace(RequestModel.ConfirmPassword))
            {
                ConfirmPasswordErrorMessage = "Please enter confirm password";
                return;
            }

            if (!DoesPasswordMatch)
            {
                return;
            }
            ShowLoader = true;
            var registerResponse = await AuthService.RegisterAsync<BaseResponse<RegisterHttpResponse>>(RequestModel);

            if (registerResponse.IsSuccess)
            {
                var loginRequest = new LoginHttpRequest()
                {
                    Email = RequestModel.Email,
                    Password = RequestModel.Password
                };

                var loginResponse = await AuthService.LoginAsync<BaseResponse<LoginHttpResponse>>(loginRequest);
                if (!loginResponse.IsSuccess && loginResponse.RedirectToPackages && loginResponse.Data is not null && loginResponse.Data.Token is not null)
                {
                    await TokenProvider.SetTokenAsync(loginResponse.Data.Token);
                    navManager.NavigateTo($"/Pricing?isNewUser=true&newUserName={RequestModel.Name}");
                    return;
                }

                Snackbar.Add(!string.IsNullOrWhiteSpace(loginResponse.Message) ? loginResponse.Message : "Something went wrong while letting you in.", Severity.Error);
            }

            ShowLoader = false;
            ResponseError =  registerResponse.Message;
        }

        private void IsStrongPassword(ChangeEventArgs input)
        {
            Password = input?.Value?.ToString() ?? "";
        }

        private void HandleConfirmPassword(ChangeEventArgs e)
        {
            RequestModel.ConfirmPassword = e?.Value?.ToString() ?? "";

            if (!string.Equals(RequestModel.ConfirmPassword, RequestModel.Password, StringComparison.Ordinal))
            {
                DoesPasswordMatch = false;
                ConfirmPasswordErrorMessage = "Password do not match";
            }
            else
            {
                ConfirmPasswordErrorMessage = string.Empty;
                DoesPasswordMatch = true;
            }
        }

        public void HandlePasswordToggle(bool passwordToggle)
        {
            if (passwordToggle)
            {
                ShowPassword = !ShowPassword;
                PasswordType =  ShowPassword ? "text" : "password";
                return;
            }

            ShowConfirmPassword = !ShowConfirmPassword;
            PasswordConfirmType =  ShowConfirmPassword ? "text" : "password";
        }
    }
}
