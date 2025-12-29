using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Models.SignalR;
using FBMMultiMessenger.Notification;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OneSignalSDK.DotNet;
using System.Text.Json;


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
        private OneSignalService OneSignalService { get; set; }

        [Inject]
        private ICurrentUserService CurrentUserService { get; set; }


        [SupplyParameterFromQuery]
        public string IsNotification { get; set; } //this bit tells if the user opens the notification from his app and we have to show him the right chat.

        [SupplyParameterFromQuery]
        public string FbChatId { get; set; }


        //For Media files
        private const int MaxMediaCount = 35;
        private const int MaxMediaSize = 25 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 25mb.

        //The actual message 
        private string Message = string.Empty;
        private string? UserProfileImage;

        private List<FileData> PreviewMediaFiles { get; set; } = new List<FileData>();
        private List<FileData> PreviewMediaInMessagesContainer = new List<FileData>();
        private bool IsNotified = false;
        private bool IsLoading = true;

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

        //Selected Message Header
        private string? selectedListingTitle;
        private string? selectedListingImage;
        private string? selectedListingLocation;
        private string? selectedListingPrice;



        //Carousel
        private MudCarousel<string> _carousel = null!;

        public bool _arrows { get; set; } = true;

        public bool _bullets { get; set; } = true;

        public bool _enableSwipeGesture { get; set; } = true;

        public bool _autocycle { get; set; } = false;
        public List<FileData> _carouselItems { get; set; } = new List<FileData>();
        public bool _showCarousel;

        public List<GetMyChatsHttpResponse> FilteredAccountChats = new List<GetMyChatsHttpResponse>();
        public List<GetMyChatsHttpResponse> AccountChats = new List<GetMyChatsHttpResponse>();

        public List<GeChatMessagesHttpResponse> ChatMessages = new List<GeChatMessagesHttpResponse>();

        protected override async Task OnInitializedAsync()
        {
            AddEventListneres();

            CurrentUser = await CurrentUserService.GetCurrentUser() ?? new();



            //if a user opened notification so we have to open the right chat.
            if (!string.IsNullOrWhiteSpace(IsNotification) && !string.IsNullOrWhiteSpace(FbChatId))
            {
                await LoadChatMessage(FbChatId);
            }

            var taskSignalR = ConnectToSignalR();

            var taskAccountsQuery = GetAccountChats();

            await Task.WhenAll(taskSignalR, taskAccountsQuery);

            await ConfigurePushNotificationsAsync();

            await JS.InvokeVoidAsync("registerEnterHandler", DotNetObjectReference.Create(this));
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

            HandleSelectedChat(fbChatId);

            if (PlatformHelper.IsMobilePlatform)
            {
                HandleMobileSideBar();
                await HandlePushNotification(fbChatId);
            }

            var myAccountChats = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (myAccountChats is not null)
            {
                //Making the unread messages to read
                myAccountChats.UnReadCount = 0;
                myAccountChats.IsRead = true;
                UserProfileImage = myAccountChats.UserProfileImage;
                await InvokeAsync(StateHasChanged);
            }

            var response = await ChatMessagesService.GetChatMessages<BaseResponse<List<GeChatMessagesHttpResponse>>>(fbChatId);

            if (response is null || !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);
                SelectedFbChatId = previousSelectedChatId;
                return;
            }

            var responseChatMesasge = response?.Data ?? new List<GeChatMessagesHttpResponse>();

            foreach (var chatMessge in responseChatMesasge)
            {
                if (chatMessge.IsImageMessage || chatMessge.IsVideoMessage)
                {
                    chatMessge.FileData = GetFileData(chatMessge.Message);
                }
            }

            ChatMessages  = response?.Data ?? new List<GeChatMessagesHttpResponse>();
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

            if (PreviewMediaFiles.Count > 0)
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

                FilesMessage.FileData = PreviewMediaFiles;

                messages.Add(FilesMessage);
                ChatMessages.Add(FilesMessage);

                PreviewMediaFiles = new();

                foreach (var file in PreviewMediaFiles)
                {
                    JS.InvokeVoidAsync("myInterop.revokePreviewUrl", file.PreviewUrl);
                }
            }

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
            var response = await AccountService.GetMyChatsAsync();

            IsLoading = false;
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

            if (!SignalRService.IsConnected)
            {
                await SignalRService.ConnectAsync(currentUserId);

            }
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
        }

        private async Task HandleAccountStatusChangedAsync(List<AccountStatusSignalRModel> accounts)
        {
            if (accounts is null || accounts.Count == 0) return;

            var accountStatusMap = accounts.ToDictionary(x => x.AccountId);

            var hasChanges = false;

            foreach (var chat in FilteredAccountChats)
            {
                if (chat.Account is not null
                    && accountStatusMap.TryGetValue(chat.Account.Id, out var status))
                {
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
        private async Task ConfigurePushNotificationsAsync()
        {
            if (PlatformHelper.IsMobilePlatform)
            {
                await OneSignalService.AskNotificationPermissionAsync();
                OneSignalService.OnNotificationClicked();

                // Optional
                var playerId = OneSignal.User.PushSubscription.Id;
                Console.WriteLine("Player Id :", playerId);
            }
        }

        public async Task HandlePushNotification(string fbChatId)
        {
            if (PlatformHelper.IsMobilePlatform)
            {
                var deviceId = OneSignal.User.PushSubscription.Id;
                await SignalRService.HandleNotification(deviceId, fbChatId);
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


        private List<FileData> GetFileData(string message)
        {
            var mediaUrls = JsonSerializer.Deserialize<List<string>>(message);

            var fileModel = mediaUrls?.Select(url => new FileData()
            {
                PreviewUrl = url,
                IsVideo = IsVideo(url)

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

            HandleMobileMainChat();
            StateHasChanged();
        }

        private void HandleSelectedChat(string fbChatId)
        {
            // Updates the main chat header with the listing details (title,image, location, and price)
            // of the chat selected by the user.

            var chat = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (chat is not null)
            {
                selectedListingTitle = chat.FbListingTitle;
                selectedListingLocation = chat.FbListingLocation;
                selectedListingPrice  = chat.FbListingPrice?.ToString();
                selectedListingImage = chat.FbListingImage;
            }
        }

        private void HandleMobileMainChat()
        {
            // Displays the sidebar view on mobile by resetting the selected chat
            // and bringing the sidebar to the front.
            SelectedFbChatId = null;
            SidebarZIndex = 100;
            MainChatZIndex = 0;
        }

        private void HandleMobileSideBar()
        {
            // Displays the main chat view on mobile by bringing the chat section
            // to the front and hiding the sidebar.
            SidebarZIndex = 0;
            MainChatZIndex = 110;

        }

        private void ShowCarousal(List<FileData> selectedChatFiles, string selectedChatFileUrl, bool isVideo = false)
        {
            _showCarousel = true;

            var shallowCopy = selectedChatFiles.Select(x => new FileData()
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
                                          .ToList();

            var remainingFileData = allFileData.Where(x => !shallowCopy.Any(s => s.PreviewUrl == x.PreviewUrl)).ToList();

            _carouselItems.AddRange(remainingFileData);
        }

        private void FilterChat()
        {
            var filteredAccountChats = AccountChats.Where(x => x.FbListingTitle.ToLower().Contains(FilterKeyword)
                                        ||
                                        x.FbListingPrice.ToString().Contains(FilterKeyword)
                                        ||
                                        x.FbListingLocation.ToLower().Contains(FilterKeyword)).ToList();

            FilteredAccountChats = new List<GetMyChatsHttpResponse>(filteredAccountChats);

            StateHasChanged();

        }

        private void CloseCarousel()
        {
            _showCarousel = false;
        }

        public async Task HandleFileUpload(InputFileChangeEventArgs e)
        {
            try
            {
                var files = e.GetMultipleFiles();

                var options = new SweetAlertOptions();
                options.Title = "Failed to upload files";
                options.ConfirmButtonText = "Close";

                if (files.Count > MaxMediaCount)
                {
                    options.Message = $"You can only attach {MaxMediaCount} files.";
                    await JS.InvokeAsync<bool>("myInterop.showSweetAlert", options);
                    return;
                }

                var totalSize = files.Sum(f => f.Size);

                if (totalSize > MaxMediaSize)
                {
                    options.Message = $"The files you have selected is too large,The maximum size is {MaxMediaSize / (1024 * 1024)}MB.";
                    await JS.InvokeVoidAsync("myInterop.handleMediaFailed", options);
                    return;
                }

                var previews = await JS.InvokeAsync<List<FileData>>("myInterop.createPreviewUrlsFromInput");

                foreach (var preview in previews)
                {
                    var browserFile = files[preview.Index];

                    var newFile = new FileData()
                    {
                        Id = $"File-{Guid.NewGuid()}",
                        File = browserFile,
                        Name = preview.Name,
                        PreviewUrl = preview.PreviewUrl,
                        IsVideo = preview.IsVideo
                    };

                    PreviewMediaFiles.Add(newFile);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Failed to select your file", Severity.Error);
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
