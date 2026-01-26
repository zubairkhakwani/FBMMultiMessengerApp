using FBMMultiMessenger.Contracts.Contracts.Payment;
using FBMMultiMessenger.Contracts.Contracts.Pricing;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

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

        [SupplyParameterFromQuery]
        public bool IsNewUser { get; set; }

        [SupplyParameterFromQuery]
        public string NewUserName { get; set; } = string.Empty;

        [SupplyParameterFromQuery]
        public string RedirectReason { get; set; } = string.Empty;

        public List<PricingList> DisplayedPricings { get; set; } = new List<PricingList>();
        public List<GetAllPricingHttpResponse> PricingsSoruce { get; set; } = new List<GetAllPricingHttpResponse>();

        public List<string> PreviewSelectedImages { get; set; } = new List<string>();

        public int _accountsInput = 1;
        public int AccountsInput
        {
            get => _accountsInput;
            set
            {
                _accountsInput = value;
                CalculatePricing();
            }
        }
        public decimal PricePerAccount { get; set; }
        public decimal TotalCost { get; set; }

        public BillingCylce CurrentBillingCycle = BillingCylce.Monthly;

        public decimal BasePrice { get; set; }

        public string AccountNo = "1234567890";
        public string IBAN = "PK12 ABCD 0000 1234 5678 90";
        public const int MaxMediaSize = 5 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 5mb.

        public string ResponseMessage { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool IsSubmitting { get; set; }

        public bool IsPricingLoading { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsPricingLoading = true;

            await ShowNotificationFromQueryAsync();

            var response = await PaymentService.GetMyStatus();

            if (response.IsSuccess && response.Data is not null && response.Data.Status == PaymentStatus.Rejected)
            {
                var options = new SweetAlertOptions
                {
                    Title = "Attention",
                    Message = response.Data.Description,
                    Icon = "error",
                    ConfirmButtonText = "Okay",
                    Footer = new SweetAlertFooter()
                    {
                        Text = "Check your email further assistance.",
                    }
                };

                await JS.InvokeVoidAsync("myInterop.showSweetAlert", options);
            }

            var pricingResponse = await PricingService.GetAll();
            PricingsSoruce = pricingResponse.Data ?? new List<GetAllPricingHttpResponse>();

            IsPricingLoading = false;

            GetBillingCyclePrice();
        }



        private async Task ShowNotificationFromQueryAsync()
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
            RequestModel.AccountsPurchased = AccountsInput;
            RequestModel.BillingCylce = CurrentBillingCycle;

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

        public void CalculatePricing()
        {
            var pricingSource = PricingsSoruce
                                            .FirstOrDefault(p => AccountsInput >= p.MinAccounts
                                                            &&
                                                            AccountsInput <= p.MaxAccounts);

            var displayedPricing = DisplayedPricings
                                            .FirstOrDefault(p => AccountsInput >= p.MinAccounts
                                                            &&
                                                            AccountsInput <= p.MaxAccounts);

            if (pricingSource is null || displayedPricing is null)
            {
                PricePerAccount = 0;
                TotalCost = 0;
                return;
            }

            PricePerAccount = CurrentBillingCycle switch
            {
                BillingCylce.Monthly => pricingSource.MonthlyPricePerAccount,
                BillingCylce.SemiAnnual => pricingSource.SemiAnnualPricePerAccount,
                BillingCylce.Annual => pricingSource.AnnualPricePerAccount,
                _ => 0
            };

            TotalCost = PricePerAccount * AccountsInput;

            DisplayedPricings.ForEach(p => p.IsActive = false);

            displayedPricing.IsActive = true;
        }

        private void HandleBillingCyleChanged(BillingCylce selectedBillingCycle)
        {
            if (CurrentBillingCycle == selectedBillingCycle) return;

            GetBillingCyclePrice(selectedBillingCycle);
        }

        private void GetBillingCyclePrice(BillingCylce billingCylce = BillingCylce.Monthly)
        {
            DisplayedPricings = PricingsSoruce
                .Select(p => new PricingList
                {
                    MinAccounts = p.MinAccounts,
                    MaxAccounts = p.MaxAccounts,
                    PricePerAccount = billingCylce switch
                    {
                        BillingCylce.Monthly => p.MonthlyPricePerAccount,
                        BillingCylce.SemiAnnual => p.SemiAnnualPricePerAccount,
                        BillingCylce.Annual => p.AnnualPricePerAccount,
                        _ => p.MonthlyPricePerAccount
                    },
                })
                .ToList();

            CurrentBillingCycle = billingCylce;

            CalculatePricing();
        }


        private async Task HandleCopyToClipboardAsync(bool isCopyIBAN = false)
        {
            var text = isCopyIBAN ? IBAN : AccountNo;
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

    }

    public class PricingList
    {
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal PricePerAccount { get; set; }
        public bool IsActive { get; set; }
    }
}
