using FBMMultiMessenger.Contracts.Contracts.Account;

namespace FBMMultiMessenger.Models
{
    public class ChatPanelContext
    {
        public List<GetMyChatsHttpResponse> SidebarChats { get; set; } = new();
        public string? SelectedFbChatId { get; set; }
        public string? RecipientProfileImage { get; set; }
        public ChatHeaderContext HeaderContext { get; set; } = new();
    }
    public class ChatHeaderContext
    {
        public bool IsAccountConnected { get; set; }
        public string? AccountName { get; set; }
        public string? ListingTitle { get; set; }
        public string? ListingLocation { get; set; }
        public string? ListingPrice { get; set; }
        public string? ListingImage { get; set; }

        public string? RecipientName { get; set; }
        public string? RecipientId { get; set; }
    }
}
