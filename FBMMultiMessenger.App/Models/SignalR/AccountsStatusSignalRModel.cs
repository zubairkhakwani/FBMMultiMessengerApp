namespace FBMMultiMessenger.Models.SignalR
{
    public class AccountStatusSignalRModel
    {
        public int AccountId { get; set; }

        public string AuthStatus { get; set; } = string.Empty;
        public string ConnectionStatus { get; set; } = string.Empty;
    }
}
