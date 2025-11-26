using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Account
{
    public partial class UpsertAccount
    {
        public UpsertAccountHttpRequest model { get; set; } = new();
        public PopupFormSettings popupFormSettings { get; set; }

        [Parameter]
        public string? AccountId { get; set; }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public string Cookie { get; set; }


        [Inject]
        public IAccountService AccountService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        public IMudDialogInstance mudDialog { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }

        private bool IsMobilePlatform = DeviceInfo.Platform != DevicePlatform.WinUI;

        private string Title = "Create Account";
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
                    Title = $"{(AccountId is null ? "Add" : "Edit")} Account",
                    Icon = Icons.Material.TwoTone.Payments,
                    ContentHeight = "390px",
                    PopupView = PopupView.Single
                };
            }
            if (AccountId is not null)
            {
                model.Name = Name;
                model.Cookie = Cookie;
                Title = "Edit Account";
                SubTitle = "Update your account details";
                Heading = "Update Information";
                Description = "Modify your account information below";
                ButtonText = "Update Account";
            }
        }

        public async Task OnValidSubmit()
        {
            var isValidReqeust = ValidateCookie(model.Cookie);
            if (!isValidReqeust)
            {
                Snackbar.Add("The cookie you provided is not valid. Please provide a valid facebook cookie.", Severity.Info);
                return;
            }

            int? accountId = string.IsNullOrWhiteSpace(AccountId) ? null : Convert.ToInt32(AccountId);

            var response = await AccountService.UpsertAccountAsync<BaseResponse<UpsertAccountHttpResponse>>(model, accountId);

            mudDialog?.Close(DialogResult.Ok(true));

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
            }

            else if (response.Data is not null &&  !response.Data.IsEmailVerified)
            {
                Navigation.NavigateTo($"/verify-otp?ReturnUrl=/Account&ReturnTo=Account&OtpSuccessMessage={response.Message}&EmailSentTo={response.Data.EmailSendTo}&IsEmailVerification=true");
            }

            else if (response.RedirectToPackages)
            {
                Navigation.NavigateTo($"/Pricing?redirectReason={response.Message}");
            }

            else
            {
                Snackbar.Add(response.Message, response.IsSuccess ? Severity.Success : Severity.Error);

                if (IsMobilePlatform && response.IsSuccess)
                {
                    Navigation.NavigateTo("/Account");
                }
            }
        }

        public void Cancel()
        {
            mudDialog.Cancel();
        }

        private bool ValidateCookie(string cookieString)
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
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
