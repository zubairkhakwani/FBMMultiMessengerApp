using Microsoft.AspNetCore.Components;

namespace FBMMultiMessenger.Components.Pages.Mobile
{
    public partial class MobileNav
    {
        [Inject] private NavigationManager Navigation { get; set; }


        private void NavigateTo(string path)
        {
            var currentPath = new Uri(Navigation.Uri).AbsolutePath;
            if (currentPath != path)
            {
                Navigation.NavigateTo(path);
            }
        }

        private bool IsActive(string path)
        {
            var currentPath = new Uri(Navigation.Uri).AbsolutePath;
            if (currentPath == "/" || currentPath == "")
                currentPath = "/Chat";

            return currentPath.Equals(path, StringComparison.OrdinalIgnoreCase);
        }
    }
}
