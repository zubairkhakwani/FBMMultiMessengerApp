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
using OneSignalSDK.DotNet.Core.Notifications;
using SixLabors.ImageSharp;
using System.Text.Json;
using System.Text.RegularExpressions;


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
        public string FbChatId { get; set; } = string.Empty;


        //For Media files
        private const int MaxMediaCount = 35;
        private const int MaxMediaSize = 25 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 25mb.

        //The actual message 
        private string Message = string.Empty;
        private string? UserProfileImage;

        private List<FileData> PreviewMediaFiles { get; set; } = new List<FileData>();
        private bool isCompressingMedia = false;
        private bool IsChatsLoading = true;

        private string? SelectedFbChatId = null;
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
        private bool ShowScrollToBottomButton = true;

        //Selected Message Header
        private string? selectedAccountChat;
        private bool isSelectedAccountConnected;
        private string? selectedListingTitle;
        private string? selectedListingImage;
        private string? selectedListingLocation;
        private string? selectedListingPrice;



        //Carousel
        public bool _showCarousel;
        public List<FileData> _carouselItems { get; set; } = new List<FileData>();


        //Sidebar Chats
        public List<GetMyChatsHttpResponse> AccountChats = new List<GetMyChatsHttpResponse>();
        public List<GetMyChatsHttpResponse> FilteredAccountChats = new List<GetMyChatsHttpResponse>();


        //Main Chat Messages
        public List<GeChatMessagesHttpResponse> ChatMessages = new List<GeChatMessagesHttpResponse>();

        private CancellationTokenSource _cts = new();

        protected override async Task OnInitializedAsync()
        {
            AddEventListneres();

            CurrentUser = await CurrentUserService.GetCurrentUser() ?? new();

            _ = ConnectToSignalR();

            await GetAccountChats();

            await OpenRightChat();

            //this function is okay here, as it needs to be called after a sec after rendering..
            await JS.InvokeVoidAsync("registerEnterHandler", DotNetObjectReference.Create(this));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ConfigurePushNotifications();
        }




        #region Domain Logic

        private async Task LoadChatMessage(string fbChatId)
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

            UpdateChatHeader(fbChatId);

            var myAccountChats = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (myAccountChats is not null)
            {
                //Making the unread messages to read
                myAccountChats.UnReadCount = 0;
                myAccountChats.IsRead = true;
                UserProfileImage = myAccountChats.UserProfileImage;
                await InvokeAsync(StateHasChanged);
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

        }


        private async Task NotifyLocalServer(string msg)
        {
            if (PreviewMediaFiles.Count == 0 && string.IsNullOrWhiteSpace(msg)) return;

            msg = msg.Trim();

            var messages = new List<GeChatMessagesHttpResponse>();

            if (!string.IsNullOrWhiteSpace(msg))
            {
                var textMessage = new GeChatMessagesHttpResponse
                {
                    FBChatId = SelectedFbChatId!,
                    Message = msg,
                    IsReceived = false,
                    IsSent = true,
                    IsTextMessage = true,
                    IsImageMessage = false,
                    IsVideoMessage = false,
                    IsAudioMessage = false,
                    CreatedAt = DateTime.UtcNow,
                    Sending = true,
                    UniqueId = Guid.NewGuid().ToString()
                };

                messages.Add(textMessage);
                ChatMessages.Add(textMessage);
            }

            Message = string.Empty;

            var videos = PreviewMediaFiles.Where(m => m.IsVideo).ToList();
            var otherMediaMessages = PreviewMediaFiles.Where(m => !m.IsVideo).ToList();

            if (otherMediaMessages.Count > 0)
            {
                var FilesMessage = new GeChatMessagesHttpResponse()
                {
                    FBChatId = SelectedFbChatId!,
                    Message = string.Empty,
                    IsReceived = false,
                    IsSent = true,
                    IsTextMessage = false,
                    IsImageMessage = true,
                    IsVideoMessage = false,
                    IsAudioMessage = false,
                    CreatedAt = DateTime.UtcNow,
                    Sending = true,
                    UniqueId = Guid.NewGuid().ToString()
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
                    FBChatId = SelectedFbChatId!,
                    Message = string.Empty,
                    IsReceived = false,
                    IsSent = true,
                    IsTextMessage = false,
                    IsImageMessage = false,
                    IsVideoMessage = true,
                    IsAudioMessage = false,
                    CreatedAt = DateTime.UtcNow,
                    Sending = true,
                    UniqueId = Guid.NewGuid().ToString()
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
                    FbChatId = SelectedFbChatId!,
                    Message = chat.Message,
                    Files = chat.FileData.Select(x => x.File).ToList(),
                    OfflineUniqueId = chat.UniqueId
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
                    var messageFailedToSend = ChatMessages.FirstOrDefault(x => x.UniqueId == response.Data.OfflineUniqueId);

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
            var response = await AccountService.GetMyChatsAsync(_cts.Token);

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
            var chatExistInSidebar = FilteredAccountChats.Any(x => x.FbChatId == receivedChat.FbChatId);
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
                    IsRead = false,
                };

                FilteredAccountChats.Insert(0, newChat);
            }

            if (notificationSound)
            {
                await JS.InvokeVoidAsync("myInterop.playNotificationSound", 1);
            }

            var chat = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == receivedChat.FbChatId) ?? new GetMyChatsHttpResponse();

            chat.MessagePreview = receivedChat.MessagPreview;
            chat.SenderName = receivedChat.MessagePreviewFrom;
            chat.FbListingImage = receivedChat.FbListingImage;
            chat.FbListingTitle = receivedChat.FbListingTitle ?? string.Empty;
            chat.IsAccountConnected = true;
            chat.IsRead = receivedChat.FbChatId == SelectedFbChatId;

            FilteredAccountChats.Remove(chat);
            chat.UnReadCount += receivedChat.IsReceived ? 1 : 0;
            FilteredAccountChats.Insert(0, chat);

            await InvokeAsync(StateHasChanged);


            if (receivedChat.FbChatId == SelectedFbChatId)
            {
                Snackbar.Add("Handle New Message");
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
                    if (SelectedFbChatId == chat.FbChatId)
                    {
                        isSelectedAccountConnected = status.IsConnected;
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

                OneSignal.Notifications.Clicked -= HandleNotificationClicked;
                OneSignal.Notifications.Clicked += HandleNotificationClicked;
            }
        }

        public void HandleNotificationClicked(object sender, NotificationClickedEventArgs e)
        {
            var data = e.Notification.AdditionalData;
            var hasFbChatIdKey = data.TryGetValue("chatId", out var fbChatIdObj);
            var hasSubscriptionExpiredKey = data.TryGetValue("isSubscriptionExpired", out var subscriptionExpiredObj);
            var messageKey = data.TryGetValue("message", out var message);

            if (data != null && hasFbChatIdKey && hasSubscriptionExpiredKey)
            {
                string fbChatId = fbChatIdObj!.ToString()!;
                bool isParsed = bool.TryParse(subscriptionExpiredObj!.ToString(), out bool isSubscriptionExpired);

                string CurrentRoute = "/" + Navigation.ToBaseRelativePath(Navigation.Uri).Split('?')[0];

                // Navigate to chat if subscription is not expired otherwise navigate to subscription page.
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    //if subscription has expired
                    if (isSubscriptionExpired)
                    {
                        Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={message}");
                    }
                    //if the user is another chat or in the chat sidebar
                    else if (fbChatId != SelectedFbChatId)
                    {
                        await LoadChatMessage(fbChatId);
                    }
                    // The user is not in the chat page so we have to redirect
                    else if (!CurrentRoute.Equals("/Chat", StringComparison.OrdinalIgnoreCase) || !CurrentRoute.Equals("/", StringComparison.OrdinalIgnoreCase))
                    {
                        Navigation.NavigateTo($"/chat?isNotification=true&fbChatId={fbChatId}");
                    }
                });
            }
        }

        private async Task OpenRightChat()
        {
            //if a user opened notification so we have to open the right chat.
            if (!string.IsNullOrWhiteSpace(IsNotification) && !string.IsNullOrWhiteSpace(FbChatId))
            {
                await LoadChatMessage(FbChatId);
                return;
            }

            var pendingLink = Preferences.Get("PendingDeepLink", string.Empty);

            if (!string.IsNullOrEmpty(pendingLink))
            {
                Preferences.Remove("PendingDeepLink");
                Navigation.NavigateTo(pendingLink);
            }
        }

        #endregion



        #region Helper Methods

        private void AddEventListneres()
        {
            BackButtonService.BackButtonPressed+= OnBackButtonPressed;
            SignalRService.OnAccountStatusChange += HandleAccountStatusChangedAsync;
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
            _showCarousel = false;
            ShowSidebarView();
            StateHasChanged();

            JS.InvokeVoidAsync("myInterop.stopAllMedia");
        }

        private void UpdateChatHeader(string fbChatId)
        {
            // Updates the main chat header with the listing details (title,image, location, and price)
            // of the chat selected by the user.

            var chat = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (chat is not null)
            {
                isSelectedAccountConnected = chat.IsAccountConnected;
                selectedAccountChat = chat.Account?.Name;
                selectedListingTitle = chat.FbListingTitle;
                selectedListingLocation = chat.FbListingLocation;
                selectedListingPrice  = chat.FbListingPrice?.ToString();
                selectedListingImage = chat.FbListingImage;
            }
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

            //JS.InvokeVoidAsync("hideArrowDownBtn");
        }

        private void ShowCarousal(List<FileData> selectedChatFiles, string selectedChatFileUrl, bool isVideo = false)
        {
            _showCarousel = true;

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
                //StateHasChanged();
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

            //StateHasChanged();
        }


        private void CloseCarousel()
        {
            _showCarousel = false;
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

        #endregion


        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            BackButtonService.BackButtonPressed -= OnBackButtonPressed;
            SignalRService.OnHandleMessage -= HandleMessageReceivedAsync;
            SignalRService.OnAccountStatusChange -= HandleAccountStatusChangedAsync;

            foreach (var file in PreviewMediaFiles)
            {
                JS.InvokeVoidAsync("myInterop.revokePreviewUrl", file.PreviewUrl);
            }
        }
    }
}
