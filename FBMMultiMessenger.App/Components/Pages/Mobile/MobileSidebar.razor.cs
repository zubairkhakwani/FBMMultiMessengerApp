using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;

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

        [Inject]
        private IAppService AppService { get; set; }


        public string FullName = string.Empty;
        public string ShortName { get; set; } = string.Empty;

        private void HandleUpdateClick()
        {
            if (PlatformHelper.IsMobilePlatform)
            {
                AppService.UpdateAndriodApk();
            }
            else
            {
                // Desktop update (future)  
            }
        }


        public async Task Logout()
        {
            await AuthService.Logout();
        }
    }
}
