using FBMMultiMessenger.Components.Pages.Shared.CustomPopupform;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Models.SignalR;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
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
        private IDialogService DialogService { get; set; }

        [Inject]
        public SignalRService SignalRService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }

        private GetMyAccountsHttpRequest RequestModel = new GetMyAccountsHttpRequest();

        private HashSet<GetMyAccountsHttpResponse> selectedAccounts
        {
            get;
            set;

        } = new();

        private List<GetMyAccountsHttpResponse> AccountsData = new List<GetMyAccountsHttpResponse>();
        private string? Keyword { get; set; }


        private readonly bool IsMobilePlatform = DeviceInfo.Platform != DevicePlatform.WinUI;

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

            SignalRService.OnAccountStatusChange += HandleAccountStatusChanged;
        }

        private async Task<TableData<GetMyAccountsHttpResponse>> ServerReload(TableState state, CancellationToken token)
        {
            int totalItems = 0;
            RequestModel.PageNo = state.Page > 0 ? state.Page + 1 : 1;
            RequestModel.PageSize = state.PageSize;
            RequestModel.Keyword = Keyword;

            var response = await AccountService.GetMyAccountsAsync(RequestModel);

            if (response.IsSuccess && response.Data is not null)
            {
                AccountsData = response.Data.Records;
                totalItems = response.Data.TotalCount;
            }
            else
            {
                Snackbar.Add(response?.Message ?? "Something went wrong when wrong while fetching your accounts details", Severity.Error);
            }

            table.Items = AccountsData;
            return new TableData<GetMyAccountsHttpResponse>() { TotalItems = totalItems, Items = AccountsData };
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

        private async Task HandleAccountStatusChanged(AccountsStatusSignalRModel request)
        {
            var accountStatus = request.AccountStatus;

            if (accountStatus is null || !accountStatus.Any())
                return;

            var accountsToUpdate = AccountsData.Where(a => accountStatus.Keys.Any(id => id == a.Id))
                                               .ToList();

            foreach (var account in accountsToUpdate)
            {
                account.Status = accountStatus[account.Id];
            }

            await InvokeAsync(StateHasChanged);
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

        public async Task HandleImportFile(InputFileChangeEventArgs e)
        {
            var file = e.File;

            var isValid = await ValidateImportFile(file);

            if (!isValid)
                return;

            var options = new SweetAlertOptions
            {
                Title = "Confirm Import",
                Message = "Do you want to import this file?",
                Icon = "info",
                ConfirmButtonText = "Yes, import it!",
                ShowCancelButton = true,
                CancelButtonText = "Cancel"
            };

            bool isConfirmed = await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);

            if (!isConfirmed)
                return;


            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                Snackbar.Add("The selected file is empty. Please upload a file that contains accounts data.", Severity.Warning);
                return;
            }

            try
            {
                List<UpsertAccountHttpRequest> accounts = ParseCsv(content);
                var totalCount = accounts.Count;
                var isInValidCookie = false;
                for (int i = 0; i < accounts.Count; i++)
                {
                    var account = accounts[i];

                    var (isValidCookie, userId) = ValidateCookie(account.Cookie);

                    if (!isValidCookie)
                    {
                        accounts.RemoveAt(i);
                        isInValidCookie = true;
                        i--;
                    }
                }

                if (isInValidCookie && accounts.Count == 0)
                {
                    Snackbar.Add("No valid accounts to import", Severity.Info);
                    return;
                }

                //Call Api
                var response = await AccountService.Import(accounts);

                if (response.Data is not null && response.Data.IsLimitExceeded)
                {
                    var sweetAlertOptions = new SweetAlertOptions()
                    {
                        Title =  "Limit Exceeded",
                        Message = response.Message,
                        ConfirmButtonText = "Upgrade now",
                        ShowCancelButton = true,
                        CancelButtonText = "Later"
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

                if (response.IsSuccess)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                    table?.ReloadServerData();
                    return;
                }

                Snackbar.Add(string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong when importing accounts, please try later." : response.Message, Severity.Error);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Unable to import the file. Please check that the file is not empty and has a valid format.", Severity.Error);
            }
        }

        public async Task EditAccountAsync(int accountId, string Name, string Cookie, int proxyId)
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
            parameters.Add("ProxyId", proxyId.ToString());

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
                selectedAccounts = new HashSet<GetMyAccountsHttpResponse>();
                return;
            }

            Snackbar.Add("Please select any account to delete", Severity.Info);
        }

        public async Task OpenBrowser(int accountId)
        {
            var response = await AccountService.OpenInBrowserAsync<BaseResponse<object>>(accountId);
            Snackbar.Add($"{response.Message}", response.IsSuccess ? Severity.Success : Severity.Error);
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
                    ShowCancelButton = false,
                    CancelButtonText = string.Empty,
                    ConfirmButtonText = "OK",
                    Icon = "error",
                };

                await JS.InvokeVoidAsync("myInterop.showSweetAlert", sweetAlertOptions);
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

        private List<UpsertAccountHttpRequest> ParseCsv(string content)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var accounts = new List<UpsertAccountHttpRequest>();

            // Skip header row (first line)
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 2)
                {
                    accounts.Add(new UpsertAccountHttpRequest
                    {
                        Name = parts[0].Trim(),
                        Cookie = parts[1].Trim(),
                        ProxyId = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])
                                                    ? parts[2].Trim()
                                                    : null
                    });
                }
            }

            return accounts;
        }


        private (bool isValid, string? userId) ValidateCookie(string cookieString)
        {
            try
            {
                // Parse cookies into dictionary
                var cookies = cookieString
                    .Split(';')
                    .Select(x => x.Trim().Split('=', 2))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1]);


                if (!cookies.ContainsKey("c_user") || !cookies.ContainsKey("xs"))
                    return (false, null);

                return (true, cookies["c_user"]);
            }
            catch
            {
                return (false, null);
            }
        }

        public static string GetBadgeClass(string status)
        {
            return status switch
            {
                "Active" => "account-status-badge account-status-active",
                "Inactive" => "account-status-badge account-status-inactive",
                "In Progress" => "account-status-badge account-status-inprogress",
                _ => "account-status-badge"
            };
        }
        #endregion
    }
}
