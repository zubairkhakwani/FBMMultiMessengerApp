using FBMMultiMessenger.Helpers;

namespace FBMMultiMessenger.Components.Layout
{
    public partial class TopBar
    {
        public string FullName = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            FullName = UserHelper.Name;
            Avatar = UserHelper.Avatar;
        }

        private string GetWelcomeText()
        {
            return $"Welcome, {FullName}";
        }
    }
}
