using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Payment
{
    public class AddPaymentProofHttpRequest
    {
        [Required(ErrorMessage = "Please provide payment proof")]
        public List<IBrowserFile> PaymentImages { get; set; }
        public int AccountsPurchased { get; set; }
        public decimal PurchasedPrice { get; set; }

        public string? Note { get; set; }
    }
    public class AddPaymentProofHttpResponse
    {

    }
}
