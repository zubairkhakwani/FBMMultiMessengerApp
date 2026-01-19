namespace FBMMultiMessenger.Models
{
    internal class NotificationAdditionalData
    {
        public string FbChatId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsSubscriptionExpired { get; set; }
    }
}
