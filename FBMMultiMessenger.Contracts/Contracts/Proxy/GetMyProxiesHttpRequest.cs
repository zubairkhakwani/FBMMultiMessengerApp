using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Contracts.Contracts.Proxy
{
    public class GetMyProxiesHttpRequest : PageableRequest
    {

    }
    public class GetMyProxiesHttpResponse
    {
        public int Id { get; set; }

        public  string Ip_Port { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public  string Password { get; set; } = string.Empty;
        public bool IsActives { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
