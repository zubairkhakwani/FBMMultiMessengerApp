using FBMMultiMessenger.Contracts.Enums;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Payment
{
    public class AddPaymentProofHttpRequest
    {
        public int PricingTierId { get; set; } //selected pricing tier id

        [Required(ErrorMessage = "Please provide payment proof")]
        public List<IBrowserFile> PaymentImages { get; set; }
        public decimal PurchasedPrice { get; set; }
        public BillingCylce BillingCylce { get; set; }
        public string? Note { get; set; }
    }
    public class AddPaymentProofHttpResponse
    {

    }
}
