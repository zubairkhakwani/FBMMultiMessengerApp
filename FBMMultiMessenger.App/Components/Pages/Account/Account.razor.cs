using FBMMultiMessenger.Components.Shared.CustomPopupform;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;


namespace FBMMultiMessenger.Components.Pages.Account
{
    public partial class Account
    {
        [Inject]
        private IAccountService AccountService { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }
        [Inject]
        private ITokenProvider TokenProvider { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        private bool IsMobilePlatform = DeviceInfo.Platform != DevicePlatform.WinUI;

        [SupplyParameterFromQuery]
        public string? Message { get; set; }

        private MudTable<GetMyAccountsHttpResponse> table;

        protected override async Task OnInitializedAsync()
        {
            string? token = await TokenProvider.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                Navigation.NavigateTo("/login");
            }

            if (!string.IsNullOrWhiteSpace(Message))
            {
                Snackbar.Add(Message, Severity.Success);
            }
        }

        private async Task<TableData<GetMyAccountsHttpResponse>> ServerReload(TableState state, CancellationToken token)
        {
            var response = await AccountService.GetMyAccountsAsync<BaseResponse<List<GetMyAccountsHttpResponse>>>();
            int totalItems = 0;
            List<GetMyAccountsHttpResponse> data = new List<GetMyAccountsHttpResponse>();
            if (response.IsSuccess && response.Data is not null)
            {
                data = response.Data;
                totalItems = response.Data.Count;
            }
            else
            {
                Snackbar.Add(response?.Message ?? "Something went wrong when wrong while fetching your accounts details", Severity.Error);
            }

            table.Items = data;
            return new TableData<GetMyAccountsHttpResponse>() { TotalItems = totalItems, Items = data };
        }
        public async Task AddNewAccountAsync()
        {
            if (IsMobilePlatform)
            {
                Navigation.NavigateTo("/create/account");
                return;
            }

            var parameters = new DialogParameters();

            var result = await DialogService.Show<UpsertAccount>("", parameters).Result;

            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }

        public async Task EditAccountAsync(int accountId, string Name, string Cookie)
        {
            if (IsMobilePlatform)
            {
                Navigation.NavigateTo($"/edit/{accountId}/account/{Name}/{Cookie}");
                return;
            }

            var parameters = new DialogParameters();
            parameters.Add("AccountId", accountId.ToString());
            parameters.Add("Name", Name);
            parameters.Add("Cookie", Cookie);

            var result = await DialogService.Show<UpsertAccount>("", parameters).Result;
            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }

        public async Task RemoveAccountAsync(int accountId)
        {
            var parameters = new DialogParameters();
            parameters.Add("ContentText", $"Do you want to delete this account?");
            parameters.Add("ButtonText", $"Delete it");
            parameters.Add("Color", Color.Primary);

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

            var dialog = await DialogService.Show<ConfirmationDialog>($"Do you want to delete this account?", parameters, options).Result;

            if (dialog.Canceled)
            {
                return;
            }

            var resposne = await AccountService.RemoveAccountAsync<BaseResponse<RemoveAccountHttpResponse>>(accountId);

            if (resposne is not null && resposne.IsSuccess)
            {
                Snackbar.Add(resposne.Message, Severity.Success);
                await table.ReloadServerData();
            }
            else
            {
                Snackbar.Add(resposne?.Message ?? "Something went wrong, please try later", Severity.Error);
            }
        }

        public void OpenBrowser(int accountId)
        {
            AccountService.OpenInBrowserAsync<object>(accountId);
            Snackbar.Add("The account has been opened in your browser.", Severity.Success);
        }
    }
}
