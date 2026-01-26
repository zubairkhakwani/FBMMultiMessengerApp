using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class ChatSidebar : IDisposable
    {
        [Inject]
        public IAccountService AccountService { get; set; }

        [Inject]
        public ChatEventDispatcherService ChatEvent { get; set; }

        [Inject]
        public BackButtonService BackButtonService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        public bool IsChatsLoading { get; set; }

        public string? SelectedFbChatId;

        //For Mobile layout we overlap the side bar and messages
        private int SidebarZIndex = 100;

        public List<GetMyChatsHttpResponse> SourceChats { get; set; } = new();
        public List<GetMyChatsHttpResponse> FilteredChats = new List<GetMyChatsHttpResponse>();


        private string _filterKeyword = string.Empty;
        private string FilterKeyword
        {
            get => _filterKeyword;
            set
            {
                _filterKeyword = value;
                FilterChat();
            }
        }

        private CancellationTokenSource _cts = new();
        protected override async Task OnInitializedAsync()
        {
            ChatEvent.OnNewChatReceived += HandleNewChat;
            ChatEvent.OnNewChatUpdated += HandleChatUpdated;

            ChatEvent.OnLayoutChanged += ShowSidebarView;
            //BackButtonService.BackButtonPressed += HandleBackButtonPressed;

            await GetAccountChats();
        }

        private async Task ShowSidebarView(int sidebarZIndex, int mainChatZIndex)
        {
            SelectedFbChatId = null;
            SidebarZIndex = sidebarZIndex;

            await InvokeAsync(StateHasChanged);
            //JS.InvokeVoidAsync("hideArrowDownBtn");
        }

        public async Task GetAccountChats()
        {
            IsChatsLoading = true;

            var response = await AccountService.GetMyChatsAsync(_cts.Token);

            IsChatsLoading = false;

            if (response is null ||  !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

                return;
            }

            SourceChats = response?.Data?.Chats ?? new List<GetMyChatsHttpResponse>();

            FilteredChats = SourceChats.ToList();

            ChatEvent.RaiseChatLoaded(SourceChats.ToList());
        }

        private async Task OnChatSelected(string fbChatId)
        {
            var chat = FilteredChats.FirstOrDefault(x => x.FbChatId == fbChatId);

            if (chat is null) return;

            MarkChatAsRead(chat);

            if (PlatformHelper.IsMobilePlatform)
            {
                SidebarZIndex = 0; // Send the sidebar to back for mobile layout
                ChatEvent.RaiseLayoutChanged(0, 110);
                await InvokeAsync(StateHasChanged);
            }

            NotifyChatSelected(chat, fbChatId);
        }

        private void NotifyChatSelected(GetMyChatsHttpResponse chat, string selectedFbChatId)
        {
            var chatPanelContext = new ChatPanelContext();

            chatPanelContext.SelectedFbChatId = selectedFbChatId;
            chatPanelContext.RecipientProfileImage = chat.UserProfileImage;

            chatPanelContext.HeaderContext.IsAccountConnected = chat.IsAccountConnected;
            chatPanelContext.HeaderContext.AccountName = chat.Account?.Name;
            chatPanelContext.HeaderContext.ListingTitle = chat.FbListingTitle;
            chatPanelContext.HeaderContext.ListingLocation = chat.FbListingLocation;
            chatPanelContext.HeaderContext.ListingPrice = chat.FbListingPrice?.ToString();
            chatPanelContext.HeaderContext.ListingImage = chat.FbListingImage;
            chatPanelContext.HeaderContext.RecipientName = chat.ChattingWithName;
            chatPanelContext.HeaderContext.RecipientId =  chat.ChattingWithId;

            ChatEvent.RaiseChatSelected(chatPanelContext);
        }

        private async Task HandleNewChat(GetMyChatsHttpResponse newChat)
        {
            FilteredChats.Insert(0, newChat);

            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleChatUpdated(HandleChatHttpResponse updatedChat)
        {
            var chat = FilteredChats.FirstOrDefault(x => x.FbChatId == updatedChat.FbChatId) ?? new GetMyChatsHttpResponse();

            chat.MessagePreview = updatedChat.MessagPreview;
            chat.SenderName = updatedChat.MessagePreviewFrom;
            chat.FbListingImage = updatedChat.FbListingImage;
            chat.FbListingTitle = updatedChat.FbListingTitle ?? string.Empty;
            chat.IsAccountConnected = true;
            chat.IsRead = updatedChat.FbChatId == SelectedFbChatId;

            FilteredChats.Remove(chat);
            chat.UnReadCount += updatedChat.IsReceived ? 1 : 0;
            FilteredChats.Insert(0, chat);

            await InvokeAsync(StateHasChanged);
        }

        private void MarkChatAsRead(GetMyChatsHttpResponse selecetedChat)
        {
            selecetedChat.UnReadCount = 0;
            selecetedChat.IsRead = true;
        }

        private void HandleBackButtonPressed()
        {
            // if on the sidebar navigate back to account page 
            if (SidebarZIndex > 0)
            {
                Navigation.NavigateTo("/Account");
                return;
            }

            SidebarZIndex = 100;
            StateHasChanged();
        }


        private void FilterChat()
        {
            var keyword = FilterKeyword.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                FilteredChats = SourceChats.ToList();
                return;
            }

            FilteredChats = SourceChats
                .Where(x =>
                    (!string.IsNullOrEmpty(x.FbListingTitle) &&
                     x.FbListingTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase))

                    || (x.FbListingPrice != null &&
                        x.FbListingPrice.ToString()!.Contains(keyword, StringComparison.OrdinalIgnoreCase))

                    || (!string.IsNullOrEmpty(x.FbListingLocation) &&
                        x.FbListingLocation.Contains(keyword, StringComparison.OrdinalIgnoreCase))

                    || x.Account != null &&
                        (!string.IsNullOrEmpty(x.Account.Name) &&
                         x.Account.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                )
                .ToList();
        }

        public void Dispose()
        {
            ChatEvent.OnNewChatReceived -= HandleNewChat;
            ChatEvent.OnNewChatUpdated -= HandleChatUpdated;
            ChatEvent.OnLayoutChanged -= ShowSidebarView;


            //BackButtonService.BackButtonPressed -= HandleBackButtonPressed;
        }
    }
}
