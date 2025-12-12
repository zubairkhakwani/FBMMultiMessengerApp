using FBMMultiMessenger.Components.Pages.Account;
using FBMMultiMessenger.Contracts.Contracts.Proxy;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Proxy
{
    public partial class Proxy
    {
        [Inject]
        private IProxyService ProxyService { get; set; }

        [Inject]
        private IDialogService DialogService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }


        private GetMyProxiesHttpRequest RequestModel = new GetMyProxiesHttpRequest();

        private List<GetMyProxiesHttpResponse> ProxiesData = new List<GetMyProxiesHttpResponse>();
        private string? Keyword { get; set; }


        private readonly bool IsMobilePlatform = DeviceInfo.Platform != DevicePlatform.WinUI;
        private MudTable<GetMyProxiesHttpResponse> table;

        private async Task<TableData<GetMyProxiesHttpResponse>> ServerReload(TableState state, CancellationToken token)
        {
            int totalItems = 0;
            RequestModel.PageNo = state.Page > 0 ? state.Page + 1 : 1;
            RequestModel.PageSize = state.PageSize;
            RequestModel.Keyword = Keyword;

            var response = await ProxyService.GetMyProxiesAsync(RequestModel);

            if (response.IsSuccess && response.Data is not null)
            {
                ProxiesData = response.Data.Records;
                totalItems = response.Data.TotalCount;
            }
            else
            {
                Snackbar.Add(response?.Message ?? "Something went wrong when wrong while fetching your proxies", Severity.Error);
            }

            table.Items = ProxiesData;
            return new TableData<GetMyProxiesHttpResponse>() { TotalItems = totalItems, Items = ProxiesData };
        }

        public async Task AddNewProxyAsync()
        {
            if (IsMobilePlatform)
            {
                //Navigation.NavigateTo("/create/account");
                return;
            }

            var parameters = new DialogParameters();

            var result = await DialogService.Show<UpsertProxy>("", parameters).Result;

            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }

        public async Task EditProxyAsync(int proxyId, string IpPort, string Name, string Password)
        {
            if (IsMobilePlatform)
            {
                Navigation.NavigateTo($"/edit/{proxyId}/proxy/{IpPort}/{Name}/{Password}");
                return;
            }

            var parameters = new DialogParameters();
            parameters.Add("ProxyId", proxyId.ToString());
            parameters.Add("IpPort", IpPort);
            parameters.Add("Name", Name);
            parameters.Add("Password", Password);

            var result = await DialogService.Show<UpsertProxy>("", parameters).Result;
            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }
    }
}
