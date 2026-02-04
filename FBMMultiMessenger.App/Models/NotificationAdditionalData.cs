namespace FBMMultiMessenger.Models
{
    internal class NotificationAdditionalData
    {
        public int ChatId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsSubscriptionExpired { get; set; }
    }
}
