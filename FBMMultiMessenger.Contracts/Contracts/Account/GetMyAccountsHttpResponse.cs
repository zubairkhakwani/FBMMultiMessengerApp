using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class GetMyAccountsHttpResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
