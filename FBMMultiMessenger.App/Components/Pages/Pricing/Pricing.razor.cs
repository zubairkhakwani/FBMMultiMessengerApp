using FBMMultiMessenger.Contracts.Contracts.Payment;
using FBMMultiMessenger.Contracts.Contracts.Pricing;
using FBMMultiMessenger.Contracts.Enums;
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

        public List<GetAllPricingHttpResponse> Pricings { get; set; } = new List<GetAllPricingHttpResponse>();

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

        public decimal BasePrice { get; set; }

        public string AccountNo = "1234567890";
        public string IBAN = "PK12 ABCD 0000 1234 5678 90";
        public const int MaxMediaSize = 5 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 5mb.

        public string ResponseMessage { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool IsSubmitting { get; set; }
        protected override async Task OnInitializedAsync()
        {
            var response = await PaymentService.GetMyStatus();

            if (response.IsSuccess && response.Data is not null && response.Data.Status == PaymentStatus.Rejected)
            {
                await JS.InvokeVoidAsync("myInterop.showSweetAlert", "Attention", response.Data.Description, true, "check your email for detail information or contact support", "#", "error", "Okay");
            }


            var pricingResponse = await PricingService.GetAll();
            Pricings = pricingResponse.Data ?? new List<GetAllPricingHttpResponse>();
            BasePrice = Pricings.FirstOrDefault()?.PricePerAccount ?? 0;
            CalculatePricing();
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


        #region Helper Methods

        private void CalculatePricing()
        {
            var priceObj = Pricings
                                  .FirstOrDefault(x => AccountsInput >= x.MinAccounts
                                                  &&
                                                  AccountsInput <= x.MaxAccounts)  ?? new GetAllPricingHttpResponse();

            PricePerAccount = priceObj.PricePerAccount;
            TotalCost  = AccountsInput * PricePerAccount;

            Pricings.ForEach(p => p.IsActive = false);

            priceObj.IsActive = true;
        }

        private decimal GetDiscount(decimal price)
        {
            var discountedPrice = ((BasePrice - price) / BasePrice) * 100;

            return discountedPrice;
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
                    await JS.InvokeVoidAsync(
                      "myInterop.showSweetAlert",
                      "Invalid File",
                      "Invalid file format. Only JPG, PNG, and PDF files are allowed.",
                      false,
                      string.Empty,
                      string.Empty,
                      "error",
                      "OK",
                      false,
                      string.Empty
                   );
                    return false;
                }

                if (file.Size > MaxMediaSize)
                {
                    await JS.InvokeVoidAsync(
                        "myInterop.showSweetAlert",
                        "File Too Large",
                        $"The selected file exceeds the maximum allowed size of {MaxMediaSize / (1024 * 1024)} MB.",
                        false,
                        string.Empty,
                        string.Empty,
                        "error",
                        "OK",
                        false,
                        string.Empty
                    );
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
