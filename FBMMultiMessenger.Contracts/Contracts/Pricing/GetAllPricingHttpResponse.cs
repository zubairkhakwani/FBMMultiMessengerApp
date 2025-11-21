namespace FBMMultiMessenger.Contracts.Contracts.Pricing
{
    public class GetAllPricingHttpResponse
    {
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal PricePerAccount { get; set; }
        public bool IsActive { get; set; } // Frontend-only flag; API never sets or receives this value.
    } 
}
