using FBMMultiMessenger.Models;
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
        public string ShortName { get; set; } = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            var response = await ProfileService.GetMyProfileAsync();

            if (response.IsSuccess && response.Data is not null)
            {
                var profile = response.Data;

                FullName = profile.Name;
                ShortName = String.Join("", FullName.Split(" ")
                                                 .Select(x => x[0])
                                                 .ToList());
            }
        }

        private string GetWelcomeText()
        {
            return string.IsNullOrWhiteSpace(FullName)
                ? "Welcome back!"
                : $"Welcome back, {FullName}";
        }

        private string GetShortName()
        {
            if (!string.IsNullOrWhiteSpace(ShortName))
                return ShortName;

            return "?";
        }
    }
}
