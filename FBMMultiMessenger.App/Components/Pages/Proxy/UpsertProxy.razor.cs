using FBMMultiMessenger.Contracts.Contracts.Proxy;
using FBMMultiMessenger.Helpers;
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
        public string? IpPort { get; set; }

        [Parameter]
        public string? Name { get; set; }

        [Parameter]
        public string? Password { get; set; }


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


        private string Title = "Create Proxy";
        private string SubTitle = "Add a new proxy to your account";
        private string Heading = "Create Proxy";
        private string Description = "Enter the required details to register a new proxy.";
        private string ButtonText = "Create Proxy";


        protected override void OnInitialized()
        {
            if (!PlatformHelper.IsMobilePlatform)
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

                Title = "Update Proxy";
                SubTitle = "Update your existing proxy details";
                Heading = "Update Information";
                Description = "Update the fields below to apply changes to this proxy.";
                ButtonText = "Save Changes";
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

            if (PlatformHelper.IsMobilePlatform && response.IsSuccess)
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
