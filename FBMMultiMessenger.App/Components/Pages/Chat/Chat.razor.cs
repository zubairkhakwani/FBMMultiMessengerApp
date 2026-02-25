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
using OneSignalSDK.DotNet;
using SixLabors.ImageSharp;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;


namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class Chat : IDisposable
    {

        [Inject]
        public IAccountService AccountService { get; set; }

        [Inject]
        public IChatMessagesService ChatMessagesService { get; set; }

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
        public int? ChatId { get; set; }

        [SupplyParameterFromQuery]
        public string? TrialAvailed { get; set; }

        [SupplyParameterFromQuery]
        public string? TrialDuration { get; set; }

        [SupplyParameterFromQuery]
        public string? TrialAccounts { get; set; }

        [SupplyParameterFromQuery]
        public string? NewUserName { get; set; }

        //For Media files
        private const int MaxMediaCount = 35;
        private const int MaxMediaSize = 25 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 25mb.

        //The actual message 
        private string Message = string.Empty;
        private string? UserProfileImage;

        private List<FileData> PreviewMediaFiles { get; set; } = new List<FileData>();
        private bool isCompressingMedia = false;
        private bool IsChatsLoading = true;

        private int? SelectedChatId = null;
        private string? ActionsMenuMessageKey = null; // for showing/hiding the menu

        private string? ReplyToMessageKey = null; // for storing reply target


        private CurrentUser CurrentUser = new();

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

        //For Mobile layout we overlap the side bar and messages
        private int SidebarZIndex = 100;
        private int MainChatZIndex = 0;

        //Selected Message Header
        private string? SelectedAccountName;
        private string? ChattingWithName;
        private string? ChattingWithId;
        private bool IsSelectedChatsAccountConnected;
        private string? SelectedChatListingTitle;
        private string? SelectedChatListingImage;
        private string? SelectedChatListingLocation;
        private string? SelectedChatListingPrice;

        //Chat Menu Action
        private bool ShowChatMenuAction;


        private bool ShowMessageReply;
        private string MessageReply = string.Empty;
        private string MessageReplyTo = string.Empty;

        //Carousel
        private bool ShowCarousel;
        public List<FileData> _carouselItems { get; set; } = new List<FileData>();


        //Sidebar Chats
        public List<GetMyChatsHttpResponse> AccountChats = new List<GetMyChatsHttpResponse>();
        public List<GetMyChatsHttpResponse> FilteredAccountChats = new List<GetMyChatsHttpResponse>();


        //Main Chat Messages
        public List<GeChatMessagesHttpResponse> ChatMessages = new List<GeChatMessagesHttpResponse>();

        private CancellationTokenSource _apiCts = new();
        private CancellationTokenSource _holdCts = new();

        protected override async Task OnInitializedAsync()
        {
            AddEventListneres();

            CurrentUser = await CurrentUserService.GetCurrentUser() ?? new();

            _ = ConnectToSignalR();

            await GetAccountChats();

            await HandleQueryParameters();

            await HandleNotificationDeepLinkAsync();

            //this function is okay here, as it needs to be called after a sec after rendering..
            await JS.InvokeVoidAsync("registerEnterHandler", DotNetObjectReference.Create(this), PlatformHelper.IsMobilePlatform);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ConfigurePushNotifications();
        }


        #region Domain Logic

        private async Task LoadChatMessage(int chatId)
        {
            var previousSelectedChatId = SelectedChatId;
            SelectedChatId = chatId;

            if (previousSelectedChatId != SelectedChatId)
            {
                ChatMessages.Clear();
            }

            if (PlatformHelper.IsMobilePlatform)
            {
                ShowMainChatView();
            }

            UpdateChatHeader(chatId);
            CloseMessageActionMenu();
            ShowMessageReply = false;

            var myAccountChats = FilteredAccountChats.FirstOrDefault(x => x.ChatId == chatId);

            if (myAccountChats is not null)
            {
                //Making the unread messages to read
                myAccountChats.UnReadCount = 0;
                myAccountChats.IsRead = true;
                UserProfileImage = myAccountChats.UserProfileImage;
            }

            var response = await ChatMessagesService.GetChatMessages(chatId, _apiCts.Token);

            if (response is null || !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);
                SelectedChatId = previousSelectedChatId;
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


        private async Task NotifyLocalServer(string msg)
        {
            if (SelectedChatId is null || (PreviewMediaFiles.Count == 0 && string.IsNullOrWhiteSpace(msg))) return;

            msg = msg.Trim();

            var messages = new List<GeChatMessagesHttpResponse>();

            var fbMessageReplyId = GetFbMessageReplyId(ReplyToMessageKey);

            var replyData = HandleMessageReply(ReplyToMessageKey);

            var messageReply = replyData.messageReply;
            var messageReplyTo = replyData.messageReplyTo;


            if (!string.IsNullOrWhiteSpace(msg))
            {
                var textMessage = new GeChatMessagesHttpResponse
                {
                    ChatId = SelectedChatId.Value,
                    Message = msg,
                    FbMessageReplyId = fbMessageReplyId,
                    //MessageReply = messageReply,
                    //MessageReplyTo = messageReplyTo,
                    IsReceived = false,
                    IsSent = true,
                    IsTextMessage = true,

                    IsImageMessage = false,
                    IsVideoMessage = false,
                    IsAudioMessage = false,
                    CreatedAt = DateTime.UtcNow,
                    Sending = true,
                    OfflineUniqueId = Guid.NewGuid().ToString()
                };

                messages.Add(textMessage);
                ChatMessages.Add(textMessage);
            }

            Message = string.Empty;
            ReplyToMessageKey = null;
            ShowMessageReply = false;

            var videos = PreviewMediaFiles.Where(m => m.IsVideo).ToList();
            var otherMediaMessages = PreviewMediaFiles.Where(m => !m.IsVideo).ToList();

            if (otherMediaMessages.Count > 0)
            {
                var FilesMessage = new GeChatMessagesHttpResponse()
                {
                    ChatId = SelectedChatId.Value,
                    Message = string.Empty,
                    FbMessageReplyId = fbMessageReplyId,
                    //MessageReply = messageReply,
                    //MessageReplyTo = messageReplyTo,
                    IsReceived = false,
                    IsSent = true,
                    IsTextMessage = false,
                    IsImageMessage = true,
                    IsVideoMessage = false,
                    IsAudioMessage = false,
                    CreatedAt = DateTime.UtcNow,
                    Sending = true,
                    OfflineUniqueId = Guid.NewGuid().ToString()
                };

                FilesMessage.FileData = otherMediaMessages;

                messages.Add(FilesMessage);

                ChatMessages.Add(FilesMessage);
            }

            //facebook sends videos one by one, so otid will mismatch if we send them as a single message.
            foreach (var video in videos)
            {
                var FilesMessage = new GeChatMessagesHttpResponse()
                {
                    ChatId = SelectedChatId!.Value,
                    FbMessageReplyId = fbMessageReplyId,
                    //MessageReply = messageReply,
                    //MessageReplyTo = messageReplyTo,
                    Message = string.Empty,
                    IsReceived = false,
                    IsSent = true,
                    IsTextMessage = false,
                    IsImageMessage = false,
                    IsVideoMessage = true,
                    IsAudioMessage = false,
                    CreatedAt = DateTime.UtcNow,
                    Sending = true,
                    OfflineUniqueId = Guid.NewGuid().ToString()
                };

                FilesMessage.FileData = new List<FileData>() { video };

                messages.Add(FilesMessage);

                ChatMessages.Add(FilesMessage);
            }

            PreviewMediaFiles.Clear();

            foreach (var chat in messages)
            {
                //This is to call API 
                var request = new NotifyLocalServerHttpRequest()
                {
                    ChatId = SelectedChatId.Value,
                    FbMessageReplyId = fbMessageReplyId,
                    Message = chat.Message,
                    Files = chat.FileData.Select(x => x.File).ToList(),
                    OfflineUniqueId = chat.OfflineUniqueId
                };

                var response = await LocalServerService.Notify<BaseResponse<NotifyLocalServerHttpResponse>>(request);

                if (!response.IsSuccess && response.RedirectToPackages)
                {
                    var isSubscriptionExpired = response.Data?.IsSubscriptionExpired ?? false;
                    Navigation.NavigateTo("/pricing");
                    return;
                }

                if (!response.IsSuccess && response.Data is not null)
                {
                    var messageFailedToSend = ChatMessages.FirstOrDefault(x => x.OfflineUniqueId == response.Data.OfflineUniqueId);

                    if (messageFailedToSend is not null)
                    {
                        messageFailedToSend.IsSent = false;
                    }
                    return;
                }

                if (!response.IsSuccess)
                {
                    Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);
                }
            }
        }


        public async Task GetAccountChats()
        {
            var response = await AccountService.GetMyChatsAsync(_apiCts.Token);

            IsChatsLoading = false;

            if (response is null ||  !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

                return;
            }

            FilteredAccountChats = AccountChats = response?.Data?.Chats ?? new List<GetMyChatsHttpResponse>();
        }

        #endregion



        #region SinglarR
        private async Task ConnectToSignalR()
        {
            var currentUserId = $"App_{CurrentUser.Id}";

            await SignalRService.ConnectAsync(currentUserId);

            SignalRService.OnHandleMessage -= HandleMessageReceivedAsync;
            SignalRService.OnHandleMessage += HandleMessageReceivedAsync;
        }


        //Handles chat messages
        private async Task HandleMessageReceivedAsync(HandleChatHttpResponse receivedChat)
        {
            var chatExistInSidebar = FilteredAccountChats.Any(x => x.ChatId == receivedChat.ChatId);

            var notificationSound = true;

            if (receivedChat.ChatId == SelectedChatId)
            {
                notificationSound = false;

                var chatMessage = ChatMessages.FirstOrDefault(x => !x.IsReceived && !string.IsNullOrWhiteSpace(receivedChat.OfflineUniqueId) && x.OfflineUniqueId == receivedChat.OfflineUniqueId);

                if (chatMessage is not null)
                {
                    chatMessage.Sending = false;
                    chatMessage.FbMessageId = receivedChat.FbMessageId;
                    chatMessage.FbMessageReplyId = receivedChat.FbMessageReplyId;
                }
                else
                {
                    var receivedMessage = new GeChatMessagesHttpResponse()
                    {
                        ChatMessageId = receivedChat.ChatMessageId,
                        FbMessageId  = receivedChat.FbMessageId,
                        FbMessageReplyId = receivedChat.FbMessageReplyId,
                        MessageReply = receivedChat.MessageReply,
                        IsReceived = receivedChat.IsReceived,
                        IsTextMessage = receivedChat.IsTextMessage,
                        IsImageMessage = receivedChat.IsImageMessage,
                        IsVideoMessage = receivedChat.IsVideoMessage,
                        IsAudioMessage = receivedChat.IsAudioMessage,
                        FbTimeStamp = receivedChat.FbTimestamp,
                        CreatedAt = receivedChat.CreatedAt,
                        IsSent = true,
                        ScrollToBottom = false,
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
                    ChatId = receivedChat.ChatId,
                    FbListingTitle = receivedChat.FbListingTitle ?? string.Empty,
                    FbListingImage = receivedChat.FbListingImage,
                    UserProfileImage = receivedChat.UserProfileImage,
                    FbListingLocation = receivedChat.FbListingLocation ?? string.Empty,
                    FbListingPrice = receivedChat.FbListingPrice,
                    MessagePreview = receivedChat.MessagPreview,
                    SenderName = receivedChat.MessagePreviewFrom,
                    FbChatId = receivedChat.FbChatId,
                    StartedAt = receivedChat.CreatedAt,
                    IsAccountConnected = true,
                    IsRead = false,
                };

                FilteredAccountChats.Insert(0, newChat);
            }

            if (notificationSound)
            {
                await JS.InvokeVoidAsync("myInterop.playNotificationSound", 1);
            }

            var chat = FilteredAccountChats.FirstOrDefault(x => x.ChatId == receivedChat.ChatId) ?? new GetMyChatsHttpResponse();

            chat.MessagePreview = receivedChat.MessagPreview;
            chat.SenderName = receivedChat.MessagePreviewFrom;
            chat.FbListingImage = receivedChat.FbListingImage;
            chat.FbListingTitle = receivedChat.FbListingTitle ?? string.Empty;
            chat.IsAccountConnected = true;
            chat.IsRead = receivedChat.ChatId == SelectedChatId;

            FilteredAccountChats.Remove(chat);
            chat.UnReadCount += receivedChat.IsReceived ? 1 : 0;
            FilteredAccountChats.Insert(0, chat);

            await InvokeAsync(StateHasChanged);


            if (receivedChat.ChatId == SelectedChatId)
            {
                await JS.InvokeVoidAsync("handleNewMessage");
            }
        }

        private async Task HandleAccountStatusChangedAsync(List<AccountStatusSignalRModel> accounts)
        {
            if (accounts is null || accounts.Count == 0) return;

            var accountStatusMap = accounts.ToDictionary(x => x.AccountId);

            var hasChanges = false;

            foreach (var chat in FilteredAccountChats)
            {
                if (chat.Account is not null && accountStatusMap.TryGetValue(chat.Account.Id, out var status))
                {
                    if (SelectedChatId == chat.ChatId)
                    {
                        IsSelectedChatsAccountConnected = status.IsConnected;
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
        private async Task ConfigurePushNotifications()
        {
            if (PlatformHelper.IsMobilePlatform)
            {
                //Ask user to allow notification permission
                await OneSignal.Notifications.RequestPermissionAsync(true);

                var currentUser = await CurrentUserService.GetCurrentUser();

                var currentUserId = currentUser.Id;

                var oneSignalExternalId = $"FBM_{currentUserId}";

                OneSignal.Login(oneSignalExternalId);
            }
        }

        private async Task OnNotificaitonClicked(NotificationAdditionalData notification)
        {
            var notificaitonFbChatId = notification.ChatId;

            //If the user is on different chat or on sidebar, then load the chat messages
            if (notificaitonFbChatId !=0 && notificaitonFbChatId != SelectedChatId)
            {
                await LoadChatMessage(notificaitonFbChatId);
            }
        }

        private async Task HandleQueryParameters()
        {
            // Executes when user taps a notification while the app is running
            if (!string.IsNullOrWhiteSpace(IsNotification) && ChatId != null && ChatId !=0)
            {
                await LoadChatMessage(ChatId.Value);
                return;
            }

            if (!string.IsNullOrWhiteSpace(TrialAvailed) && !string.IsNullOrWhiteSpace(TrialAccounts) && !string.IsNullOrWhiteSpace(TrialDuration))
            {
                var options = new SweetAlertOptions
                {
                    Title = $"Welcome {NewUserName}!",
                    Message =
                             "Your account has been successfully created and your free trial has been activated!\n\n" +
                             "This is a gift from us\n\n" +
                             $"Trial Duration: {TrialDuration} days\n" +
                             $"Accounts Included: {TrialAccounts}",
                    Icon = "success",
                    ConfirmButtonText = "Get Started",
                    ShowCancelButton = false
                };


                await JS.InvokeVoidAsync("myInterop.showSweetAlert", options);
            }
        }

        private async Task HandleNotificationDeepLinkAsync()
        {
            //Exectutes when app is opened from a terminated state via notification
            var pendingLink = IntentDataHelper.DeepLink;

            if (!string.IsNullOrWhiteSpace(pendingLink))
            {
                IntentDataHelper.DeepLink = null;

                var uri = new Uri(pendingLink);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);

                var chatId = Convert.ToInt32(queryParams["chatId"]);
                var messageText = queryParams["message"];
                var isSubscriptionExpiredString = queryParams["isSubscriptionExpired"];

                bool.TryParse(isSubscriptionExpiredString, out var isSubscriptionExpired);

                if (isSubscriptionExpired)
                {
                    Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={messageText}");
                }
                else if (chatId != null)
                {
                    await LoadChatMessage(chatId);
                }
            }
        }

        #endregion



        #region Helper Methods

        private void AddEventListneres()
        {
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




        [JSInvokable]
        public async Task HandleEnterKey(string message)
        {
            await NotifyLocalServer(message);
            await InvokeAsync(StateHasChanged);
        }


        public async Task HandleCopyToClipboardAsync(string message)
        {
            bool isCopied = await JS.InvokeAsync<bool>("myInterop.copyToClipboard", message);

            if (isCopied)
            {
                Snackbar.Add($"Copied to clipboard", Severity.Info);
            }
            else
            {
                Snackbar.Add($"Failed to copy", Severity.Error);
            }

            CloseMessageActionMenu();
        }

        private void HandleCancelMessageReply()
        {
            ShowMessageReply = false;
            MessageReply = string.Empty;
            ReplyToMessageKey = null;
        }

        private string? GetFbMessageReplyId(string? chatMessageKey)
        {
            var chatMessage = ChatMessages.FirstOrDefault(cm => cm.OfflineUniqueId == chatMessageKey);

            if (chatMessage is null)
            {
                chatMessage = ChatMessages.FirstOrDefault(cm => cm.ChatMessageId.ToString() == chatMessageKey);
            }


            return chatMessage?.FbMessageId;
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

        private void UpdateChatHeader(int chatId)
        {
            // Updates the main chat header with the listing details (title,image, location, and price)
            // of the chat selected by the user.

            var chat = FilteredAccountChats.FirstOrDefault(x => x.ChatId == chatId);
            if (chat is not null)
            {
                IsSelectedChatsAccountConnected = chat.IsAccountConnected;
                SelectedAccountName = chat.Account?.Name;
                SelectedChatListingTitle = chat.FbListingTitle;
                SelectedChatListingLocation = chat.FbListingLocation;
                SelectedChatListingPrice  = chat.FbListingPrice?.ToString();
                SelectedChatListingImage = chat.FbListingImage;
                ChattingWithName = chat.ChattingWithName;
                ChattingWithId = chat.ChattingWithId;
            }
        }

        private void ShowSidebarView()
        {
            // Displays the sidebar view on mobile by resetting the selected chat
            // and bringing the sidebar to the front.
            SelectedChatId = null;
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

        private void HandleChatMenuOpen()
        {
            ShowChatMenuAction = true;
        }

        private void HandleChatMenuClose()
        {
            ShowChatMenuAction = false;
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

        private void FilterChat()
        {
            var keyword = FilterKeyword.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                FilteredAccountChats = AccountChats.ToList();
                return;
            }

            FilteredAccountChats = AccountChats
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


        private async Task StartHold(int messageId, string offlineUniqueId)
        {
            if (ActionsMenuMessageKey == messageId.ToString() || ActionsMenuMessageKey == offlineUniqueId) return;

            _holdCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(500, _holdCts.Token);

                ActionsMenuMessageKey = messageId == 0 ? offlineUniqueId : messageId.ToString();

            }
            catch
            {
                // Hold was cancelled - expected behavior
            }
        }

        private void CancelHold()
        {
            _holdCts?.Cancel();
        }


        private void CloseMessageActionMenu()
        {
            ActionsMenuMessageKey = null;
        }

        private void CloseCarousel()
        {
            ShowCarousel = false;
        }

        public async Task HandleFileUpload(InputFileChangeEventArgs e)
        {
            try
            {
                var options = new SweetAlertOptions();
                options.Title = "Failed to upload files";
                options.ConfirmButtonText = "Close";

                if (e.FileCount > MaxMediaCount)
                {
                    options.Message = $"You can only attach {MaxMediaCount} files.";
                    await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);
                    return;
                }

                var files = e.GetMultipleFiles(MaxMediaCount);

                var totalSize = files.Sum(f => f.Size);

                if (totalSize > MaxMediaSize)
                {
                    options.Message = $"The files you have selected is too large,The maximum size is {MaxMediaSize / (1024 * 1024)}MB.";
                    await JS.InvokeVoidAsync("myInterop.showSweetAlert", options);
                    return;
                }

                isCompressingMedia = true;
                var previews = await JS.InvokeAsync<List<FileData>>("myInterop.previewAndCompressImages");

                for (int i = 0; i < previews.Count; i++)
                {
                    var preview = previews[i];
                    var newFile = new FileData()
                    {
                        Id = $"File-{Guid.NewGuid()}",
                        Name = preview.Name,
                        PreviewUrl = preview.PreviewUrl,
                        CompressedBytes = preview.CompressedBytes,
                        File = preview.IsVideo ? files[i] : new CompressedBrowserFile(preview.Name, preview.CompressedBytes),
                        IsVideo = preview.IsVideo,
                    };
                    PreviewMediaFiles.Add(newFile);
                }

            }
            catch (Exception ex)
            {
                Snackbar.Add("Failed to select your file", Severity.Error);
                SentrySdk.CaptureException(ex);
            }
            finally
            {
                isCompressingMedia = false;
            }

        }

        public void HandleFileRemoval(string id)
        {
            var file = PreviewMediaFiles.FirstOrDefault(x => x.Id == id);
            if (file is not null)
            {
                JS.InvokeVoidAsync("myInterop.revokePreviewUrl", file.PreviewUrl);
                PreviewMediaFiles.Remove(file);
            }
        }

        private (string? messageReply, string? messageReplyTo) HandleMessageReply(string? messageKey, bool showMessageReply = false)
        {
            ShowMessageReply = showMessageReply;

            var chatMessage = ChatMessages.FirstOrDefault(cm => cm.OfflineUniqueId == messageKey);

            if (chatMessage is null)
            {
                chatMessage = ChatMessages.FirstOrDefault(cm => cm.ChatMessageId.ToString() == messageKey);
            }

            if (chatMessage is null) return (null, null);

            if (chatMessage.IsImageMessage)
            {
                MessageReply = "Image";
            }
            else if (chatMessage.IsVideoMessage)
            {
                MessageReply = "Video";
            }
            else
            {
                MessageReply = chatMessage.Message;
            }

            MessageReplyTo = chatMessage.IsReceived ? ChattingWithName : "Yourself";

            ReplyToMessageKey = messageKey;

            CloseMessageActionMenu();

            return (MessageReply, MessageReplyTo);
        }

        private async Task ScrollToRepliedMessageAsync(string? fbmMessageReplyId)
        {
            var chatMessage = ChatMessages.FirstOrDefault(cm => cm.FbMessageId == fbmMessageReplyId);

            if (chatMessage is not null)
            {
                await JS.InvokeVoidAsync("ScrollToRepliedMessage", chatMessage.ChatMessageId);
            }
        }



        #endregion


        public void Dispose()
        {
            _apiCts.Cancel();
            _apiCts.Dispose();

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
