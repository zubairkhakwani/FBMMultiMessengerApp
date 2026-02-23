using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Models
{
    internal class NotificationAdditionalData
    {
        public NotificationCategory Category { get; set; }
        public int ChatId { get; set; }
        public int? AccountId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsSubscriptionExpired { get; set; }
    }
}
