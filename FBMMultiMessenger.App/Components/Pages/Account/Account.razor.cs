using CsvHelper;
using CsvHelper.Configuration;
using FBMMultiMessenger.Components.Pages.Shared.CustomPopupform;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Models.SignalR;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Globalization;
using System.Text;
using Color = MudBlazor.Color;


namespace FBMMultiMessenger.Components.Pages.Account
{
    public partial class Account : IDisposable
    {
        [Inject]
        private IAccountService AccountService { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private IDialogService DialogService { get; set; }

        [Inject]
        public SignalRService SignalRService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }

        private GetMyAccountsHttpRequest RequestModel = new GetMyAccountsHttpRequest();

        private HashSet<UserAccountsHttpResponse> selectedAccounts
        {
            get;
            set;

        } = new();

        private List<UserAccountsHttpResponse> AccountsData = new List<UserAccountsHttpResponse>();
        private int TotalAccounts;
        private int ConnectedAccounts;
        private int NotConnectedAccounts;

        private string? Keyword { get; set; }

        [SupplyParameterFromQuery]
        public string? Message { get; set; }

        private MudTable<UserAccountsHttpResponse> table;

        private CancellationTokenSource _cts = new();

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                Snackbar.Add(Message, Severity.Success);
            }

            SignalRService.OnAccountStatusChange -= HandleAccountStatusChangedAsync;
            SignalRService.OnAccountStatusChange += HandleAccountStatusChangedAsync;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ProcessCsvFile();
            }
        }

        private async Task<TableData<UserAccountsHttpResponse>> ServerReload(TableState state, CancellationToken token)
        {
            int totalItems = 0;
            RequestModel.PageNo = state.Page > 0 ? state.Page + 1 : 1;
            RequestModel.PageSize = state.PageSize;
            RequestModel.Keyword = Keyword;

            var response = await AccountService.GetMyAccountsAsync(RequestModel, _cts.Token);

            if (response.IsSuccess)
            {
                AccountsData = response.Data?.UserAccounts?.Records ?? [];
                TotalAccounts = totalItems = response.Data?.UserAccounts?.TotalCount ?? 0;
                ConnectedAccounts = response.Data?.ConnectedAccounts ?? 0;
                NotConnectedAccounts = response.Data?.NotConnectedAccounts ?? 0;
            }
            else
            {
                Snackbar.Add(response?.Message ?? "Something went wrong when wrong while fetching your accounts details", Severity.Error);
            }

            table.Items = AccountsData;

            if (TotalAccounts !=0)
            {
                await InvokeAsync(StateHasChanged);
            }

            return new TableData<UserAccountsHttpResponse>() { TotalItems = totalItems, Items = AccountsData };
        }

        private async Task HandleFilterClickAsync()
        {
            await table.ReloadServerData();
        }

        private async Task HandleFilterKeyPressAsync(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await HandleFilterClickAsync();
            }
        }

        private async Task HandleAccountStatusChangedAsync(List<AccountStatusSignalRModel> accountsStatusRequest)
        {
            if (accountsStatusRequest is null || !accountsStatusRequest.Any())
                return;

            var affectedAccounts = AccountsData.Where(a => accountsStatusRequest.Any(x => x.AccountId == a.Id))
                                               .ToList();

            foreach (var account in affectedAccounts)
            {
                var accountStatusRequest = accountsStatusRequest.FirstOrDefault(x => x.AccountId == account.Id);

                if (accountStatusRequest is not null)
                {
                    if (accountStatusRequest.AuthStatus is not null)
                    {
                        account.AuthStatus = accountStatusRequest.AuthStatus;
                    }
                    if (accountStatusRequest.ConnectionStatus is not null)
                    {
                        account.ConnectionStatus = accountStatusRequest.ConnectionStatus;
                    }
                }
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleFileShared()
        {
            await ProcessCsvFile();
        }

        private async Task ProcessCsvFile()
        {
            if (FileShareHelper.CsvBytes is not null)
            {
                IBrowserFile browserFile = new CompressedBrowserFile("accounts_import.csv", FileShareHelper.CsvBytes, "text/csv");

                FileShareHelper.CsvBytes = null;

                await HandleImportFile(new InputFileChangeEventArgs(new List<IBrowserFile>() { browserFile }));
            }
        }

        public async Task AddNewAccountAsync()
        {
            if (PlatformHelper.IsMobilePlatform)
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

        public async Task HandleImportFile(InputFileChangeEventArgs e)
        {
            var file = e.File;

            var isValid = await ValidateImportFile(file);

            if (!isValid)
                return;

            var options = new SweetAlertOptions();

            options.Title = "Confirm Import";
            options.Message = "Do you want to import this file?";
            options.Icon = "info";
            options.ConfirmButtonText = "Yes, import it!";
            options.ShowCancelButton = true;
            options.CancelButtonText = "Cancel";

            bool isConfirmed = await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);

            if (!isConfirmed) return;

            try
            {
                CsvParseResult result = await ParseAndValidateCsvAsync(file);

                if (!result.Success)
                {
                    options.Title = "Invalid Request";
                    options.Message = result.Message;
                    options.Icon = "error";
                    options.ConfirmButtonText = "Download Format";
                    options.ShowCancelButton = true;
                    options.CancelButtonText = "Close";

                    bool isDownloadformatRequest = await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);

                    if (isDownloadformatRequest)
                    {
                        await JS.InvokeVoidAsync("myInterop.downloadAccountsFormat");
                    }

                    return;
                }

                //Call Api
                var response = await AccountService.Import(result.Accounts);

                if (response.ShowSweetAlert && response.Data is not null && response.Data.IsLimitExceeded)
                {
                    var sweetAlertOptions = new SweetAlertOptions()
                    {
                        Title =  "Limit Exceeded",
                        Message = response.Message,
                        ConfirmButtonText = "Upgrade now",
                        ShowCancelButton = true,
                        CancelButtonText = "Later",
                    };

                    var upgradeNow = await JS.InvokeAsync<bool>("myInterop.showSweetAlert", sweetAlertOptions);

                    if (upgradeNow)
                    {
                        Navigation.NavigateTo("/Pricing");
                    }

                    return;
                }

                else if (response.RedirectToPackages)
                {
                    Navigation.NavigateTo($"/Pricing?redirectReason={response.Message}");
                }

                else if (response.Data is not null && !response.Data.IsEmailVerified)
                {
                    Navigation.NavigateTo($"/verify-otp?ReturnUrl=/Account&ReturnTo=Account&OtpSuccessMessage={response.Message}&EmailSentTo={response.Data.EmailSendTo}&IsEmailVerification=true");
                }

                var skippedAccounts = response.Data?.SkippedAccounts ?? new List<SkippedAccountHttpResponse>();

                if (response.ShowSweetAlert && response.Data is not null && skippedAccounts.Count > 0)
                {
                    var totalSkipped = skippedAccounts.Count;
                    var successCount = response.Data.SuccessfullyValidated;

                    string icon;
                    string title;

                    if (successCount > 0)
                    {
                        icon = "warning";
                        title = "Import Partially Completed";
                    }
                    else
                    {
                        icon = "error";
                        title = "Import Failed";
                    }

                    var sweetAlertOptions = new SweetAlertOptions()
                    {
                        Title = title,
                        Message = response.Message,
                        Icon = icon,
                        ConfirmButtonText = "OK",
                        ShowCancelButton = false,
                        ImportData = new ImportResultData
                        {
                            TotalProcessed = response.Data.TotalProcessed,
                            SuccessfullyValidated = response.Data.SuccessfullyValidated,
                            TotalSkipped = totalSkipped,
                            SkippedAccounts = response.Data.SkippedAccounts
                        }
                    };

                    await JS.InvokeVoidAsync("myInterop.showSweetAlert", sweetAlertOptions);

                    if (successCount > 0)
                    {
                        table?.ReloadServerData();
                    }

                    return;
                }

                if (response.IsSuccess)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                    table?.ReloadServerData();
                    return;
                }

                Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while importing accounts, please try later." : response.Message, Severity.Error);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Unable to import the file. Please check that the file is not empty and has a valid format.", Severity.Error);
            }
        }

        public async Task EditAccountAsync(int accountId, string Name, string Cookie, int? proxyId)
        {
            if (PlatformHelper.IsMobilePlatform)
            {
                Navigation.NavigateTo($"/edit/{accountId}/account/{Name}/{Cookie}");
                return;
            }

            var parameters = new DialogParameters();
            parameters.Add("AccountId", accountId.ToString());
            parameters.Add("Name", Name);
            parameters.Add("Cookie", Cookie);
            parameters.Add("ProxyId", proxyId?.ToString());

            var result = await DialogService.Show<UpsertAccount>("", parameters).Result;
            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }

        public async Task RemoveAccountAsync(List<int> accountId, bool mutlipleDelete = false)
        {
            var parameters = new DialogParameters();
            var message = mutlipleDelete ? "Do you want to delete all the selected accounts?" : "Do you want to delete this account?";
            parameters.Add("ContentText", message);
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

        public async Task HandleMultipleRemoval()
        {
            if (selectedAccounts.Any())
            {
                var selectedAccountIds = selectedAccounts.Select(x => x.Id).ToList();
                await RemoveAccountAsync(selectedAccountIds, true);
                selectedAccounts = new HashSet<UserAccountsHttpResponse>();
                return;
            }

            Snackbar.Add("Please select any account to delete", Severity.Info);
        }

        public async Task ConnectAccount(int accountId)
        {
            var response = await AccountService.Connect<BaseResponse<object>>(accountId);
            Snackbar.Add($"{response.Message}", response.IsSuccess ? Severity.Success : Severity.Error);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            SignalRService.OnAccountStatusChange -= HandleAccountStatusChangedAsync;
        }


        #region Helper Methods


        public async Task<bool> ValidateImportFile(IBrowserFile file)
        {
            var maxAllowedFile = 5 * 1024 * 1024; //5mb

            var fileExtension = Path.GetExtension(file?.Name)?.ToLowerInvariant();
            var contentType = file?.ContentType.ToLowerInvariant();

            if (file is null || (fileExtension != ".csv" && contentType != "text/csv"))
            {
                var sweetAlertOptions = new SweetAlertOptions
                {
                    Title = "Invalid File",
                    Message = "Please select an excel(.csv) file",
                    ConfirmButtonText = "Download Format",
                    ShowCancelButton = true,
                    CancelButtonText = "Okay",
                    Icon = "error",
                };

                var isDowloadFormatRequest = await JS.InvokeAsync<bool>("myInterop.showSweetAlert", sweetAlertOptions);
                if (isDowloadFormatRequest)
                {
                    await JS.InvokeVoidAsync("myInterop.downloadAccountsFormat");
                }
                return false;
            }
            if (file.Size > maxAllowedFile)
            {
                var sweetAlertOptions = new SweetAlertOptions
                {
                    Title = "File Too Large",
                    Message = $"The selected file exceeds the maximum allowed size of {maxAllowedFile / (1024 * 1024)} MB.",
                    ConfirmButtonText = "OK",
                    ShowCancelButton = false,
                    CancelButtonText = string.Empty,
                    Icon = "error"
                };

                await JS.InvokeVoidAsync("myInterop.showSweetAlert", sweetAlertOptions);
                return false;
            }

            return true;
        }

        private async Task<CsvParseResult> ParseAndValidateCsvAsync(IBrowserFile file)
        {
            var result = new CsvParseResult();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, true);

                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    PrepareHeaderForMatch = args => args.Header.ToLower().Trim()
                };

                using var csv = new CsvReader(reader, csvConfig);
                await csv.ReadAsync();
                csv.ReadHeader();

                var headers = csv?.HeaderRecord?.Select(h => h.ToLower().Trim()).ToList();

                if (headers == null || headers.Count == 0)
                {
                    result.Success = false;
                    result.Message = "CSV file has no headers. Expected headers: Name, Cookie, ProxyId (optional).";
                    return result;
                }

                if (!headers.Contains("name") && !headers.Contains("cookie"))
                {
                    result.Success = false;
                    result.Message = "CSV file has no headers. Expected headers: Name, Cookie, ProxyId (optional).";
                    return result;
                }

                if (!headers.Contains("name"))
                {
                    result.Success = false;
                    result.Message = "Missing required header: Name.";
                    return result;
                }

                if (!headers.Contains("cookie"))
                {
                    result.Success = false;
                    result.Message = "Missing required header: Cookie.";
                    return result;
                }

                var rowNumber = 1;

                await foreach (var record in csv.GetRecordsAsync<UpsertAccountHttpRequest>())
                {
                    rowNumber++;

                    if (string.IsNullOrWhiteSpace(record.Name))
                    {
                        result.Success = false;
                        result.Message = $"Row {rowNumber}: Name is required.";
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(record.Cookie))
                    {
                        result.Success = false;
                        result.Message = $"Row {rowNumber}: Cookie is required.";
                        return result;
                    }

                    result.Accounts.Add(new UpsertAccountHttpRequest
                    {
                        Name = record.Name.Trim(),
                        Cookie = record.Cookie.Trim(),
                        ProxyId = string.IsNullOrWhiteSpace(record.ProxyId)
                            ? null
                            : record.ProxyId.Trim()
                    });
                }

                if (result.Accounts.Count == 0)
                {
                    result.Success = false;
                    result.Message = "No valid data found in the CSV file.";
                    return result;
                }

                result.Success = true;
                result.Message = $"Successfully imported {result.Accounts.Count} account(s).";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "The CSV file is either empty or not in the expected format. Please check the file and try again.";
                return result;
            }
        }

        public string GetConnectionStatusBadgeClass(string status)
        {
            status = status.Trim().ToLower();
            return status switch
            {
                "online" => "account-status-badge account-status-online",
                "offline" => "account-status-badge account-status-offline",
                "starting" => "account-status-badge account-status-starting",
                _ => "account-status-badge"
            };
        }

        public string GetAuthStatusBadgeClass(string status)
        {
            status = status.Trim().ToLower();
            return status switch
            {
                "logged in" => "account-status-badge account-status-loggedin",
                "logged out" => "account-status-badge account-status-loggedout",
                "idle" => "account-status-badge account-status-idle",
                _ => "account-status-badge"
            };
        }




        //Helper class for CSV 
        public class CsvParseResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<UpsertAccountHttpRequest> Accounts { get; set; } = new();
        }

        #endregion
    }
}
