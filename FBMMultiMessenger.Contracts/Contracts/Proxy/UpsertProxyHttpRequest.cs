using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Proxy
{
    public class UpsertProxyHttpRequest
    {
        [Required(ErrorMessage ="Please enter Ip port")]
        public string Ip_Port { get; set; } = null!;

        [Required(ErrorMessage = "Please enter name")]

        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Please enter password")]
        public string Password { get; set; } = null!;
    }

    public class UpsertProxyHttpResponse
    {

    }
}
