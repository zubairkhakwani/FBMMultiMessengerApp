using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Models.SignalR;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class MessagesThread : IDisposable
    {

        [Inject]
        public IChatMessagesService ChatMessagesService { get; set; }


        [Inject]
        public ChatEventDispatcherService ChatEvent { get; set; }


        [Inject]
        public ILocalServerService LocalServerService { get; set; }

        [Inject]
        public SignalRService SignalRService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private BackButtonService BackButtonService { get; set; }


        [Inject]
        private ICurrentUserService CurrentUserService { get; set; }


        [SupplyParameterFromQuery]
        public string IsNotification { get; set; } = string.Empty; //tells if the user click on the notification

        [SupplyParameterFromQuery]
        public string FbChatId { get; set; } = string.Empty;


        //For Media files
        private const int MaxMediaCount = 35;
        private const int MaxMediaSize = 25 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 25mb.

        //The actual message 
        private string Message = string.Empty;



        private List<FileData> PreviewMediaFiles { get; set; } = new List<FileData>();
        private bool isCompressingMedia = false;

        //For Mobile layout we overlap the side bar and messages
        private int SidebarZIndex = 100;
        private int MainChatZIndex = 0;

        //Chat Menu Action
        private bool ShowChatMenuAction;

        //Carousel
        private bool ShowCarousel;
        public List<FileData> _carouselItems { get; set; } = new List<FileData>();


        //Main Chat Messages
        public List<GeChatMessagesHttpResponse> ChatMessages { get; set; } = new();

        public string? SelectedFbChatId { get; set; }

        public string? RecipientProfileImage { get; set; }

        public List<GetMyChatsHttpResponse> SidebarChats { get; set; } = new();



        private CancellationTokenSource _cts = new();

        protected override async Task OnInitializedAsync()
        {
            AddEventListneres();

            _ = ConnectToSignalR();
        }

        private async Task HandleChatSelected(ChatPanelContext chatPanelContext)
        {
            SelectedFbChatId = chatPanelContext.SelectedFbChatId;
            RecipientProfileImage = chatPanelContext.RecipientProfileImage;

            await InvokeAsync(StateHasChanged);

            if (SelectedFbChatId is not null)
            {
                await LoadChatMessages(SelectedFbChatId);
            }
        }

        private async Task HandleChatLoaded(List<GetMyChatsHttpResponse> chats)
        {
            SidebarChats = chats.ToList();
        }

        private async Task HandleMessageSend(List<GeChatMessagesHttpResponse> newChatMessages)
        {
            ChatMessages.AddRange(newChatMessages);
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleMessageFailed(string offlineUniqueId)
        {
            var messageFailedToSend = ChatMessages.FirstOrDefault(x => x.UniqueId == offlineUniqueId);

            if (messageFailedToSend is not null)
            {
                messageFailedToSend.IsSent = false;
                await InvokeAsync(StateHasChanged);
            }
        }


        #region Domain Logic

        private async Task LoadChatMessages(string fbChatId)
        {
            var previousSelectedChatId = SelectedFbChatId;
            SelectedFbChatId = fbChatId;

            if (previousSelectedChatId != SelectedFbChatId)
            {
                ChatMessages.Clear();
            }

            if (PlatformHelper.IsMobilePlatform)
            {
                ShowMainChatView();
            }

            var response = await ChatMessagesService.GetChatMessages(fbChatId, _cts.Token);


            if (response is null || !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);
                SelectedFbChatId = previousSelectedChatId;
                return;
            }

            var responseChatMessages = response?.Data ?? new List<GeChatMessagesHttpResponse>();

            foreach (var chatMessage in responseChatMessages)
            {
                if (chatMessage.IsImageMessage || chatMessage.IsVideoMessage)
                {
                    chatMessage.FileData = GetFileData(chatMessage.Message);
                }
            }

            ChatMessages = response?.Data ?? new List<GeChatMessagesHttpResponse>();

            await InvokeAsync(StateHasChanged);
        }

        #endregion



        #region SinglarR
        private async Task ConnectToSignalR()
        {
            var currentUser = await CurrentUserService.GetCurrentUser() ?? new();
            var currentUserId = $"App_{currentUser.Id}";

            await SignalRService.ConnectAsync(currentUserId);

            SignalRService.OnHandleMessage -= HandleMessageReceivedAsync;
            SignalRService.OnHandleMessage += HandleMessageReceivedAsync;
        }


        //Handles chat messages
        private async Task HandleMessageReceivedAsync(HandleChatHttpResponse receivedChat)
        {
            var chatExistInSidebar = SidebarChats.Any(x => x.FbChatId == receivedChat.FbChatId);
            var notificationSound = true;

            if (receivedChat.FbChatId == SelectedFbChatId)
            {
                notificationSound = false;

                var chatMessage = ChatMessages.FirstOrDefault(x => !x.IsReceived && !string.IsNullOrWhiteSpace(receivedChat.OfflineUniqueId) && x.UniqueId == receivedChat.OfflineUniqueId);

                if (chatMessage is not null)
                {
                    chatMessage.Sending = false;
                }
                else
                {
                    var receivedMessage = new GeChatMessagesHttpResponse()
                    {
                        IsReceived = receivedChat.IsReceived,
                        IsTextMessage = receivedChat.IsTextMessage,
                        IsImageMessage = receivedChat.IsImageMessage,
                        IsVideoMessage = receivedChat.IsVideoMessage,
                        IsAudioMessage = receivedChat.IsAudioMessage,
                        IsSent = true,
                        ScrollToBottom = false,
                        CreatedAt = receivedChat.StartedAt,
                    };

                    if (receivedChat.IsImageMessage || receivedChat.IsVideoMessage)
                    {
                        receivedMessage.FileData = GetFileData(receivedChat.Message);
                    }
                    else
                    {
                        receivedMessage.Message = receivedChat.Message;
                    }

                    ChatMessages.Add(receivedMessage);
                }
            }
            else if (!chatExistInSidebar)
            {
                var newChat = new GetMyChatsHttpResponse()
                {
                    Id = receivedChat.ChatId,
                    FbListingTitle = receivedChat.FbListingTitle ?? string.Empty,
                    FbListingImage = receivedChat.FbListingImage,
                    UserProfileImage = receivedChat.UserProfileImage,
                    FbListingLocation = receivedChat.FbListingLocation ?? string.Empty,
                    FbListingPrice = receivedChat.FbListingPrice,
                    MessagePreview = receivedChat.MessagPreview,
                    SenderName = receivedChat.MessagePreviewFrom,
                    FbChatId = receivedChat.FbChatId,
                    StartedAt = receivedChat.StartedAt,
                    IsAccountConnected = true,
                    UnReadCount = 1,
                    IsRead = false,
                };

                //Maintain the sidebar chat list
                SidebarChats.Add(newChat);

                //Notify the sidebar about the new chat
                ChatEvent.RaiseNewChat(newChat);
            }

            //Notify the sidebar about the chat update
            if (chatExistInSidebar)
            {
                ChatEvent.RaiseChatUpdated(receivedChat);
            }

            await InvokeAsync(StateHasChanged);

            if (receivedChat.FbChatId == SelectedFbChatId)
            {
                await JS.InvokeVoidAsync("handleNewMessage");
            }
        }

        private async Task HandleAccountStatusChangedAsync(List<AccountStatusSignalRModel> accounts)
        {
            if (accounts is null || accounts.Count == 0) return;

            var accountStatusMap = accounts.ToDictionary(x => x.AccountId);

            var hasChanges = false;

            foreach (var chat in SidebarChats)
            {
                if (chat.Account is not null && accountStatusMap.TryGetValue(chat.Account.Id, out var status))
                {
                    if (SelectedFbChatId == chat.FbChatId)
                    {
                        //IsSelectedChatsAccountConnected = status.IsConnected;
                    }
                    chat.IsAccountConnected = status.IsConnected;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        #endregion



        #region OneSingnal - Push Notifications

        private async Task OnNotificaitonClicked(NotificationAdditionalData notification)
        {
            var notificaitonFbChatId = notification.FbChatId;

            //If the user is on different chat or on sidebar, then load the chat messages
            if (notificaitonFbChatId != SelectedFbChatId)
            {
                await LoadChatMessages(notificaitonFbChatId);
            }
        }

        private async Task OpenNotificationChat()
        {
            // Executes when user taps a notification while the app is running
            if (!string.IsNullOrWhiteSpace(IsNotification) && !string.IsNullOrWhiteSpace(FbChatId))
            {
                await LoadChatMessages(FbChatId);
                return;
            }

            //Exectutes when app is opened from a terminated state via a notification
            var pendingLink = Preferences.Get("PendingDeepLink", string.Empty);

            if (!string.IsNullOrWhiteSpace(pendingLink))
            {
                Preferences.Remove("PendingDeepLink");

                var uri = new Uri(pendingLink);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);

                var chatId = queryParams["fbChatId"];
                var messageText = queryParams["message"];
                var isSubscriptionExpiredString = queryParams["isSubscriptionExpired"];

                bool.TryParse(isSubscriptionExpiredString, out var isSubscriptionExpired);

                if (isSubscriptionExpired)
                {
                    Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={messageText}");
                }
                else if (!string.IsNullOrEmpty(chatId))
                {
                    await LoadChatMessages(chatId);
                }
            }
        }

        #endregion



        #region Helper Methods

        private void AddEventListneres()
        {
            ChatEvent.OnChatSelected += HandleChatSelected;
            ChatEvent.OnChatLoaded+= HandleChatLoaded;
            ChatEvent.OnMessageSend += HandleMessageSend;
            ChatEvent.OnMessageFailed += HandleMessageFailed;


            BackButtonService.BackButtonPressed+= OnBackButtonPressed;
            SignalRService.OnAccountStatusChange += HandleAccountStatusChangedAsync;
            BlazorMauiCommunicator.OnNotificationClicked += OnNotificaitonClicked;
        }

        private bool IsVideo(string url)
        {
            var videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".m4v" };

            try
            {
                var uri = new Uri(url);
                var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
                return videoExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }

        private bool IsFacebookEmoji(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return Regex.IsMatch(url, @"/t39\.1997-\d+/", RegexOptions.IgnoreCase) &&
                   Regex.IsMatch(url, @"_n\.png(\?|$)", RegexOptions.IgnoreCase);
        }

        private bool IsFacebookSticker(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return Regex.IsMatch(url, @"/t39\.1997-\d+/", RegexOptions.IgnoreCase) &&
                   Regex.IsMatch(url, @"_n\.webp(\?|$)", RegexOptions.IgnoreCase);
        }


        private List<FileData> GetFileData(string message)
        {
            var mediaUrls = JsonSerializer.Deserialize<List<string>>(message);

            var fileModel = mediaUrls?.Select(url => new FileData()
            {
                PreviewUrl = url,
                IsVideo = IsVideo(url),
                IsEmoji = IsFacebookEmoji(url),
                IsSticker = IsFacebookSticker(url)

            }).ToList();

            return fileModel ?? new List<FileData>();
        }

        #endregion



        #region UI Helpers Methods
        private void OnBackButtonPressed()
        {
            // Handles the Android back button press behavior.
            // If the sidebar is currently active (visible above the main chat), navigate back to the Account page.
            // Otherwise, if the user is viewing chat messages, toggle back to the sidebar view instead.

            if (SidebarZIndex > MainChatZIndex)
            {
                Navigation.NavigateTo("/Account");
                return;
            }
            ShowCarousel = false;
            ShowSidebarView();
            StateHasChanged();

            JS.InvokeVoidAsync("myInterop.stopAllMedia");
        }

        private void ShowSidebarView()
        {
            // Displays the sidebar view on mobile by resetting the selected chat
            // and bringing the sidebar to the front.
            SelectedFbChatId = null;
            SidebarZIndex = 100;
            MainChatZIndex = 0;

            JS.InvokeVoidAsync("hideArrowDownBtn");
        }

        private void ShowMainChatView()
        {
            // Displays the main chat view on mobile by bringing the chat section
            // to the front and hiding the sidebar.
            SidebarZIndex = 0;
            MainChatZIndex = 110;
        }


        private void ShowCarousal(List<FileData> selectedChatFiles, string selectedChatFileUrl, bool isVideo = false)
        {
            ShowCarousel = true;

            var shallowCopy = selectedChatFiles.Where(f => !f.IsEmoji && !f.IsSticker).Select(x => new FileData()
            {
                Id = x.Id,
                Name = x.Name,
                File = x.File,
                PreviewUrl = x.PreviewUrl,
                IsVideo = x.IsVideo

            }).ToList();

            var selcetedFileIndex = shallowCopy.FindIndex(x => x.PreviewUrl == selectedChatFileUrl);

            shallowCopy.RemoveAt(selcetedFileIndex);

            shallowCopy.Insert(0, new FileData() { PreviewUrl = selectedChatFileUrl, IsVideo = isVideo });

            _carouselItems = shallowCopy.Select(x => new FileData()
            {
                Id = x.Id,
                Name = x.Name,
                PreviewUrl =x.PreviewUrl,
                IsVideo = x.IsVideo,
                File = x.File,

            }).ToList();

            var allFileData = ChatMessages.Where(x => x.FileData.Count > 0)
                                          .SelectMany(x => x.FileData)
                                          .Where(x => !x.IsEmoji && !x.IsSticker)
                                          .ToList();

            var remainingFileData = allFileData.Where(x => !shallowCopy.Any(s => s.PreviewUrl == x.PreviewUrl)).ToList();

            _carouselItems.AddRange(remainingFileData);
        }


        #endregion


        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            ChatEvent.OnChatSelected += HandleChatSelected;
            ChatEvent.OnChatLoaded -= HandleChatLoaded;
            ChatEvent.OnMessageSend -= HandleMessageSend;
            ChatEvent.OnMessageFailed -= HandleMessageFailed;


            BackButtonService.BackButtonPressed -= OnBackButtonPressed;
            SignalRService.OnHandleMessage -= HandleMessageReceivedAsync;
            SignalRService.OnAccountStatusChange -= HandleAccountStatusChangedAsync;
            BlazorMauiCommunicator.OnNotificationClicked -= OnNotificaitonClicked;

            foreach (var file in PreviewMediaFiles)
            {
                JS.InvokeVoidAsync("myInterop.revokePreviewUrl", file.PreviewUrl);
            }
        }
    }
}
