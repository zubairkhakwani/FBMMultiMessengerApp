using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Extension
{
    public class NotifyLocalServerHttpRequest
    {
        [Required]
        public string FbChatId { get; set; } = null!;

        public string OfflineUniqueId { get; set; } = string.Empty;
        [Required]
        public string Message { get; set; } = null!;

        public List<IBrowserFile>? Files { get; set; }

    }

    public class NotifyLocalServerHttpResponse
    {
        public bool IsSubscriptionExpired { get; set; }
        public string OfflineUniqueId { get; set; } = string.Empty;
    }
}
