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
        public string Avatar { get; set; } = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            var response = await ProfileService.GetMyProfileAsync();

            if (response.IsSuccess && response.Data is not null)
            {
                var profile = response.Data;

                var name = profile?.Name ?? "Jhon Doe";
                var splitedName = name.Trim().Split(" ");

                var avatar = name[0].ToString();

                if (splitedName.Length > 1)
                {
                    var firstLetter = splitedName[0][0];
                    var secondLetter = splitedName[1][0];

                    avatar = $"{firstLetter}{secondLetter}";
                }

                FullName = name;
                Avatar = avatar;
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
            if (!string.IsNullOrWhiteSpace(Avatar))
                return Avatar;

            return "?";
        }
    }
}
