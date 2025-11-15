namespace FBMMultiMessenger.Contracts.Contracts.Profile
{
    public class GetMyProfleHttpResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
