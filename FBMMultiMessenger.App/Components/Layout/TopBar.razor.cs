using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;

namespace FBMMultiMessenger.Components.Layout
{
    public partial class TopBar
    {
        [Inject]
        public ICurrentUserService CurrentUserService { get; set; }

        [Inject]
        public IProfileService ProfileService { get; set; }

        public string FullName = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            var response = await ProfileService.GetMyProfileAsync();

            if (response.IsSuccess && response.Data is not null)
            {
                var profile = response.Data;

                FullName= profile.Name;
                Avatar = UserHelper.GetShortName(profile.Name);
            }
        }

        private string GetWelcomeText()
        {
            return $"Welcome, {FullName}";
        }
    }
}
