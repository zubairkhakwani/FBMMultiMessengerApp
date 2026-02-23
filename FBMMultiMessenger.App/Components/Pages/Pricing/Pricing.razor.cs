using FBMMultiMessenger.Contracts.Contracts.Payment;
using FBMMultiMessenger.Contracts.Contracts.Pricing;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OneSignalSDK.DotNet;

namespace FBMMultiMessenger.Components.Pages.Pricing
{
    public partial class Pricing
    {
        public AddPaymentProofHttpRequest RequestModel { get; set; } = new();

        [Inject]
        public IPricingService PricingService { get; set; }

        [Inject]
        public IPaymentService PaymentService { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public IAuthService AuthService { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        [SupplyParameterFromQuery]
        public bool IsNewUser { get; set; }

        [SupplyParameterFromQuery]
        public string NewUserName { get; set; } = string.Empty;

        [SupplyParameterFromQuery]
        public string RedirectReason { get; set; } = string.Empty;

        public List<PricingList> DisplayedPricings { get; set; } = new List<PricingList>();
        public List<PricingTierHttpResponse> PricingsSoruce { get; set; } = new List<PricingTierHttpResponse>();
        public List<AccountDetailsHttpResponse> AccountDetails { get; set; } = new List<AccountDetailsHttpResponse>();
        public PricingTierAvailabilityHttpResponse PricingTierAvailability { get; set; } = new PricingTierAvailabilityHttpResponse();

        public List<string> PreviewSelectedImages { get; set; } = new List<string>();

        public decimal Savings { get; set; }
        public decimal TotalCost { get; set; }

        public BillingCylce? CurrentBillingCycle;

        public const int MaxMediaSize = 5 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 5mb.

        public string ResponseMessage { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool IsSubmitting { get; set; }
        public bool IsPricingLoading { get; set; }

        public bool CanSubmitPaymentProof = true;

        public PaymentStatus PaymentStatus;
        public PricingList? SelectedTier;
        protected override async Task OnInitializedAsync()
        {
            IsPricingLoading = true;

            await HandlePaymentAlertsAsync();

            await LoadPricingDataAsync();

            IsPricingLoading = false;

            GetBillingCyclePrice(CurrentBillingCycle);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (PlatformHelper.IsMobilePlatform)
            {
                await OneSignal.Notifications.RequestPermissionAsync(true);
            }
        }

        private async Task HandlePaymentAlertsAsync()
        {
            var paymentStatusResponse = await PaymentService.GetMyStatus();
            var paymentStatus = paymentStatusResponse.Data;

            if (paymentStatus == null)
                return;

            PaymentStatus = paymentStatus.Status;

            if (ShouldShowPaymentAlert(paymentStatus.Status))
            {
                await ShowPaymentAlertAsync(paymentStatus);
            }
            else
            {
                await ShowNotificationFromQueryAsync();
            }
        }

        private bool ShouldShowPaymentAlert(PaymentStatus status)
        {
            return status == PaymentStatus.Rejected || status == PaymentStatus.Pending;
        }

        private bool CanSubmitPayment()
        {
            return PaymentStatus != PaymentStatus.Pending && SelectedTier is not null;
        }

        private async Task ShowPaymentAlertAsync(GetMyVerificationStatusHttpResponse paymentStatus)
        {
            var options = new SweetAlertOptions
            {
                Title = "Attention",
                Message = paymentStatus.Description ?? "Please contact administrator",
                Icon =  paymentStatus.Status == PaymentStatus.Rejected ? "error" : "info",
                ConfirmButtonText = "Okay",
                Footer = paymentStatus.Status == PaymentStatus.Rejected
                    ? new SweetAlertFooter { Text = "Check your email for further assistance." }
                    : null
            };

            await JS.InvokeVoidAsync("myInterop.showSweetAlert", options);
        }

        private async Task LoadPricingDataAsync()
        {
            var pricingResponse = await PricingService.GetAll();
            PricingsSoruce = pricingResponse.Data?.PricingTiers ?? new List<PricingTierHttpResponse>();
            AccountDetails = pricingResponse.Data?.AccountDetails ?? new List<AccountDetailsHttpResponse>();
            PricingTierAvailability = pricingResponse.Data?.PricingTierAvailability ?? new PricingTierAvailabilityHttpResponse();


            if (PricingTierAvailability.IsMonthlyAvailable)
            {
                CurrentBillingCycle = BillingCylce.Monthly;
            }
            else if (PricingTierAvailability.IsSemiAnnualAvailable)
            {
                CurrentBillingCycle = BillingCylce.SemiAnnual;
            }
            else if (PricingTierAvailability.IsAnnualAvailable)
            {
                CurrentBillingCycle = BillingCylce.Annual;
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task ShowNotificationFromQueryAsync()
        {
            try
            {

                if (IsNewUser)
                {
                    var options = new SweetAlertOptions
                    {
                        Title = $"Welcome {NewUserName}!",
                        Message = "Your account has been successfully created. To unlock all features and start your journey, please choose a subscription plan.",
                        Icon = "success",
                        ConfirmButtonText = "Get started"
                    };
                    await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);
                }
                else if (!string.IsNullOrWhiteSpace(RedirectReason))
                {
                    var options = new SweetAlertOptions
                    {
                        Title = "Attention!",
                        Message = RedirectReason,
                        Icon = "info",
                        ConfirmButtonText = "Get started"
                    };
                    await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task HandlePaymentProofSubmitAsync()
        {
            //Extra safety check
            if (RequestModel.PaymentImages is null)
            {
                Snackbar.Add("Please provide payment proof");
                return;
            }

            IsSubmitting = true;
            RequestModel.PurchasedPrice = TotalCost;
            RequestModel.PricingTierId = SelectedTier!.Id;
            RequestModel.BillingCylce = CurrentBillingCycle ?? BillingCylce.Monthly;

            var response = await PaymentService.SubmitProof(RequestModel);

            IsSubmitting  = false;

            ResponseMessage =  string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while submitting payment proof, please try later." : response.Message;

            IsSuccess =  response.IsSuccess;

            Snackbar.Add(ResponseMessage, IsSuccess ? Severity.Success : Severity.Error);
        }

        public async Task HandlePaymentProofChangeAsync(InputFileChangeEventArgs e)
        {
            var files = e.GetMultipleFiles();

            var isValid = await ValidatePaymentProofFile(files);

            if (!isValid)
                return;

            foreach (var file in files)
            {
                using var ms = new MemoryStream();
                await file.OpenReadStream(MaxMediaSize).CopyToAsync(ms);
                var buffer = ms.ToArray();
                var base64 = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}";

                PreviewSelectedImages.Add(base64);
            }

            RequestModel.PaymentImages = files.ToList();
        }

        private void HandleBillingCyleChanged(BillingCylce selectedBillingCycle)
        {
            if (CurrentBillingCycle == selectedBillingCycle) return;

            GetBillingCyclePrice(selectedBillingCycle);
        }

        public void HandleTierSelection(int selectedTierId)
        {
            DisplayedPricings.ForEach(p => p.IsActive = false);

            var pricingSource = PricingsSoruce.FirstOrDefault(p => p.Id == selectedTierId);

            var selectedTier = DisplayedPricings.FirstOrDefault(p => p.Id == selectedTierId);

            if (pricingSource is null || selectedTier is null)
            {
                Savings = 0;
                TotalCost = 0;
                return;
            }

            selectedTier.IsActive = true;

            SelectedTier = selectedTier;

            CalculatePricing();
        }

        public void CalculatePricing()
        {
            if (SelectedTier == null) return;

            TotalCost = SelectedTier.DiscountedPrice;

            Savings = SelectedTier.OrignalPrice - SelectedTier.DiscountedPrice;
        }

        private void GetBillingCyclePrice(BillingCylce? billingCylce)
        {
            if (billingCylce is null) return;

            var month = billingCylce switch
            {
                BillingCylce.Monthly => 1,
                BillingCylce.SemiAnnual => 6,
                BillingCylce.Annual => 12,
                _ => 1
            };

            DisplayedPricings = PricingsSoruce
                .Select(p => new PricingList
                {
                    Id = p.Id,
                    UptoAccounts = p.UptoAccounts,
                    OrignalPrice = p.MonthlyPrice * month,
                    DiscountedPrice = billingCylce switch
                    {
                        BillingCylce.Monthly => p.MonthlyPrice * month,
                        BillingCylce.SemiAnnual => p.SemiAnnualPrice * month,
                        BillingCylce.Annual => p.AnnualPrice * month,
                        _ => p.MonthlyPrice * month
                    },

                    IsActive = SelectedTier is not null ? p.Id == SelectedTier.Id : false,
                })
                .ToList();

            if (SelectedTier is not null)
            {
                SelectedTier = DisplayedPricings.FirstOrDefault(x => x.Id == SelectedTier.Id);
            }


            CurrentBillingCycle = billingCylce;

            CalculatePricing();
        }

        private async Task HandleCopyToClipboardAsync(string text, bool isCopyIBAN = false)
        {
            var copiedType = isCopyIBAN ? "IBAN" : "Account Number";

            bool isCopied = await JS.InvokeAsync<bool>("myInterop.copyToClipboard", text);

            if (isCopied)
            {
                Snackbar.Add($"{copiedType} copied to clipboard", Severity.Info);
            }
            else
            {
                Snackbar.Add($"Failed to copy {copiedType}", Severity.Error);
            }
        }

        public async Task<bool> ValidatePaymentProofFile(IReadOnlyList<IBrowserFile> files)
        {
            foreach (var file in files)
            {
                var fileExtension = Path.GetExtension(file?.Name)?.ToLowerInvariant();
                var contentType = file?.ContentType.ToLowerInvariant();
                bool isValidFile =
                                (fileExtension == ".jpg"  && contentType == "image/jpeg") ||
                                (fileExtension == ".jpeg" && contentType == "image/jpeg") ||
                                (fileExtension == ".png"  && contentType == "image/png")  ||
                                (fileExtension == ".pdf"  && contentType == "application/pdf");

                if (!isValidFile || file is null)
                {
                    var sweetAlertOptions = new SweetAlertOptions
                    {
                        Title = "Invalid File",
                        Message = "Invalid file format. Only JPG, PNG, and PDF files are allowed.",
                        ConfirmButtonText = "OK",
                        ShowCancelButton = false,
                        CancelButtonText = string.Empty,
                        Icon = "error"
                    };

                    await JS.InvokeVoidAsync("myInterop.showSweetAlert", sweetAlertOptions);



                    return false;
                }

                if (file.Size > MaxMediaSize)
                {
                    var sweetAlertOptions = new SweetAlertOptions
                    {
                        Title = "File Too Large",
                        Message = $"The selected file exceeds the maximum allowed size of {MaxMediaSize / (1024 * 1024)} MB.",
                        ConfirmButtonText = "OK",
                        ShowCancelButton = false,
                        CancelButtonText = string.Empty,
                        Icon = "error"
                    };

                    await JS.InvokeVoidAsync("myInterop.showSweetAlert", sweetAlertOptions);

                    return false;
                }
            }
            return true;
        }

        private void HandleBackToDasboard()
        {
            Navigation.NavigateTo("/Account");
        }

        private async Task HandleLogout()
        {
            await AuthService.Logout();
            Navigation.NavigateTo("/login");
        }
    }

    public class PricingList
    {
        public int Id { get; set; }
        public int UptoAccounts { get; set; }
        public decimal OrignalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public bool IsActive { get; set; }
    }
}
