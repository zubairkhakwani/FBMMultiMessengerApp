using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OneSignalSDK.DotNet;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Database.Services;

namespace FBMMultiMessenger.Components.Pages.Auth
{
    public partial class Login
    {
        [SupplyParameterFromForm]
        public LoginHttpRequest RequestModel { get; set; } = new LoginHttpRequest() { Email="", Password="" };

        [Inject]
        public IAuthService AuthService { get; set; }


        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private ITokenProvider TokenProvider { get; set; }

        [Inject]
        private SyncMessagesDbService syncMessagesDbService { get; set; }

        [Inject]
        AuthenticationStateProvider AuthenticationStateProvider { get; set; }


        [Inject]
        public ISubscriptionSerivce SubscriptionSerivce { get; set; }


        public string? ResponseError;
        private bool ShowLoader = false;
        private bool ShowPassword = false;
        private string PasswordType = "password";

        public async Task OnValidPost()
        {
            ShowLoader = true;

            var response = await AuthService.LoginAsync<BaseResponse<LoginHttpResponse>>(RequestModel);

            ShowLoader = false;

            if (response.Data is not null &&  !string.IsNullOrWhiteSpace(response.Data.Token))
            {
                await TokenProvider.SetTokenAsync(response.Data.Token);
                ((CustomAuthenticationStateProvider)AuthenticationStateProvider).NotifyStateChanged();
            }

            if (!response.IsSuccess && response.RedirectToPackages)
            {
                Navigation.NavigateTo($"/pricing?redirectReason={Uri.EscapeDataString(response.Message)}", new NavigationOptions
                {
                    ReplaceHistoryEntry = true
                });
                return;
            }

            if (response.IsSuccess)
            {
                await syncMessagesDbService.WipeLocalDb();

                Navigation.NavigateTo("/Chat", new NavigationOptions
                {
                    ReplaceHistoryEntry = true
                });
                return;
            }

            ResponseError = response.Message;
        }

        public void HandlePasswordToggle()
        {
            ShowPassword = !ShowPassword;
            PasswordType =  ShowPassword ? "text" : "password";

        }
    }
}
