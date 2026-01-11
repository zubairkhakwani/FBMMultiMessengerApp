using FBMMultiMessenger.Components.Pages.Shared.CustomPopupform;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;

namespace FBMMultiMessenger.Components.Pages.DefaultMessage
{
    public partial class DefaultMessage
    {
        [Inject]
        private IAccountService AccountService { get; set; }

        [Inject]
        private IDefaultMessageService DefaultMessageService { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }
        [Inject]
        private ITokenProvider TokenProvider { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        [SupplyParameterFromQuery]
        public string? Message { get; set; }

        private MudTable<DefaultMessagesHttpResponse> table;

        private async Task<TableData<DefaultMessagesHttpResponse>> ServerReload(TableState state, CancellationToken token)
        {
            var response = await DefaultMessageService.GetMyDefaultMessagesAsync();
            int totalItems = 0;
            List<DefaultMessagesHttpResponse> data = new List<DefaultMessagesHttpResponse>();

            if (response.IsSuccess && response.Data is not null)
            {
                var defaultMessages = response.Data.DefaultMessages;

                DefaultMessageHelper.AllAccounts = response.Data.AllAccounts;

                var accountsUsedForDefaultMessages = response.Data.DefaultMessages
                                                                  .SelectMany(x => x.Accounts)
                                                                  .ToList();

                DefaultMessageHelper.AccountsUsedForDefaultMessages = DefaultMessageHelper.AllAccounts
                                               .Where(x => accountsUsedForDefaultMessages
                                               .Any(y => y.Id == x.Id))
                                               .ToList();

                DefaultMessageHelper.AccountsNotUsedForDefaultMessages = DefaultMessageHelper.AllAccounts
                                              .Where(x => !accountsUsedForDefaultMessages
                                              .Any(y => y.Id == x.Id))
                                              .ToList();


                data = response.Data.DefaultMessages;

                totalItems = response.Data.DefaultMessages.Count;
            }
            else
            {
                Snackbar.Add(response?.Message ?? "Something went wrong when wrong while fetching your accounts details", Severity.Error);
            }

            table.Items = data;
            return new TableData<DefaultMessagesHttpResponse>() { TotalItems = totalItems, Items = data };
        }
        public async Task AddDefaultMessage()
        {
            if (PlatformHelper.IsMobilePlatform)
            {
                Navigation.NavigateTo("/create/defaultmessage");
                return;
            }


            var parameters = new DialogParameters();
            var result = await DialogService.Show<UpsertDefaultMessage>("", parameters).Result;

            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }

        public async Task EditDefaultMessageAsync(int defaultMessageId, string defaultMessage, List<UserAccountsHttpResponse> selectedAccounts)
        {
            DefaultMessageHelper.SelectableAccounts = selectedAccounts;

            if (PlatformHelper.IsMobilePlatform)
            {
                Navigation.NavigateTo($"/edit/{defaultMessageId}/defaultmessage/{defaultMessage}/");
                return;
            }

            var parameters = new DialogParameters();

            parameters.Add("DefaultMessageId", defaultMessageId.ToString());
            parameters.Add("DefaultMessage", defaultMessage);

            var result = await DialogService.Show<UpsertDefaultMessage>("", parameters).Result;

            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }
    }
}
