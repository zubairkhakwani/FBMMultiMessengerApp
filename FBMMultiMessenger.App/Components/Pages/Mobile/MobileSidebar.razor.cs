using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using OneSignalSDK.DotNet;

namespace FBMMultiMessenger.Components.Pages.Mobile
{
    public partial class MobileSidebar
    {

        [Inject]
        public IAuthService AuthService { get; set; }

        [Inject]
        public ICurrentUserService CurrentUserService { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        public string FullName = string.Empty;
        public string ShortName { get; set; } = string.Empty;


        protected override async Task OnInitializedAsync()
        {
            var currentUser = await CurrentUserService.GetCurrentUser();
            var fullName = currentUser.Name;

            var shortName = UserHelper.GetShortName(fullName);

            FullName = fullName;
            ShortName = shortName;

        }
        public async Task Logout()
        {
            await AuthService.Logout();
        }
    }
}
