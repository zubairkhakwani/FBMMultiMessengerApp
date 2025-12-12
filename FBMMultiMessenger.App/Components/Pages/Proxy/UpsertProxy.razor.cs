using FBMMultiMessenger.Contracts.Contracts.Proxy;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Proxy
{
    public partial class UpsertProxy
    {
        public UpsertProxyHttpRequest model { get; set; } = new();
        public PopupFormSettings popupFormSettings { get; set; }

        [Parameter]
        public string? ProxyId { get; set; }

        [Parameter]
        public string IpPort { get; set; }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public string Password { get; set; }


        [Inject]
        public IProxyService ProxyServices { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        public IMudDialogInstance mudDialog { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }

        private bool IsMobilePlatform = DeviceInfo.Platform != DevicePlatform.WinUI;

        private string Title = "Create Proxy";
        private string SubTitle = "Join our comunity today";
        private string Heading = "Get Started";
        private string Description = "Fill in your details to create your account";
        private string ButtonText = "Create Account";

        protected override void OnInitialized()
        {
            if (!IsMobilePlatform)
            {
                popupFormSettings = new PopupFormSettings()
                {
                    Title = $"{(ProxyId is null ? "Add" : "Edit")} Proxy",
                    Icon = Icons.Material.TwoTone.Payments,
                    ContentHeight = "390px",
                    PopupView = PopupView.Single
                };
            }
            if (ProxyId is not null)
            {
                model.Name = Name;
                model.Ip_Port = IpPort;
                model.Password =  Password;

                Title = "Edit Account";
                SubTitle = "Update your account details";
                Heading = "Update Information";
                Description = "Modify your account information below";
                ButtonText = "Update Account";
            }
        }

        public async Task OnValidSubmit()
        {
            int? proxyId = string.IsNullOrWhiteSpace(ProxyId) ? null : Convert.ToInt32(ProxyId);

            var response = await ProxyServices.UpsertProxyAsync(model, proxyId);

            if (response.IsSuccess)
            {
                mudDialog?.Close(DialogResult.Ok(true));
            }

            Snackbar.Add(response.Message, response.IsSuccess ? Severity.Success : Severity.Error);

            if (IsMobilePlatform && response.IsSuccess)
            {
                Navigation.NavigateTo("/Proxy");
            }
        }

        public void Cancel()
        {
            mudDialog.Cancel();
        }

    }
}
