namespace FBMMultiMessenger.Models.SignalR
{
    public class AccountStatusSignalRModel
    {
        public int AccountId { get; set; }
        public string AuthStatusText { get; set; } = string.Empty;
        public string ConnectionStatusText { get; set; } = string.Empty;
        public string ReasonText { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
    }
}
