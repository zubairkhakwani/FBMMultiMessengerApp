using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FBMMultiMessenger.Components.Pages.Shared
{
    public partial class ChatActionMenu
    {
        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public string? ProfileName { get; set; }

        [Parameter]
        public string? ProfileId { get; set; }

        [Parameter]
        public string? ListingTitle { get; set; }

        [Parameter]
        public string? ListingId { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }

        [Inject]
        private IFacebookService FacebookService { get; set; }


        private async Task ViewUserProfile()
        {
            if (string.IsNullOrWhiteSpace(ProfileId))
            {
                var options = new SweetAlertOptions();

                options.Title = "Unable to Load Profile";
                options.Message = "We couldn’t find this profile at the moment. Please try again later.";
                options.Icon = "info";
                options.ConfirmButtonText = "OK";
                await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);
                return;
            }

            try
            {
                var link = $"https://m.facebook.com/{ProfileId}";
                await FacebookService.OpenLink(link);
            }
            catch (Exception ex)
            {
            }
        }

        private async Task ViewListing()
        {
            try
            {
                var link = $"https://www.facebook.com/marketplace/item/{ListingId}";
                await FacebookService.OpenLink(link);
            }
            catch (Exception ex)
            {
            }
        }
        private async Task CloseMenu()
        {
            await OnClose.InvokeAsync();
        }
    }
}
