using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;

namespace FBMMultiMessenger.Components.Layout
{
    public partial class TopBar
    {
        [Inject]
        public ICurrentUserService CurrentUserService { get; set; }

        public string FullName = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            var currentUser = await CurrentUserService.GetCurrentUser();
            var fullName = currentUser.Name;
            var shortName = String.Join("", fullName.Split(" ")
                                                    .Select(x => x[0])
                                                    .ToList());
            FullName = fullName;
            ShortName = shortName;

        }
    }
}
