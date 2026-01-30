using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class ChatComposer : IDisposable
    {
        [Inject]
        public ChatEventDispatcherService ChatEvent { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }


        [Inject]
        public ILocalServerService LocalServerService { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }


        public string? SelectedFbChatId;

        //For Media files
        private const int MaxMediaCount = 35;
        private const int MaxMediaSize = 25 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 25mb.

        //The actual message 
        private string Message = string.Empty;


        private List<FileData> PreviewMediaFiles { get; set; } = new List<FileData>();
        private bool isCompressingMedia = false;

        protected override async Task OnInitializedAsync()
        {
            ChatEvent.OnChatSelected += HandleChatSelected;

            await Task.Delay(200);
            await JS.InvokeVoidAsync("registerEnterHandler", DotNetObjectReference.Create(this));
        }



        private async Task HandleChatSelected(ChatPanelContext context)
        {
            SelectedFbChatId = context.SelectedFbChatId;

            await InvokeAsync(StateHasChanged);
        }

        private async Task NotifyLocalServer(string msg)
        {
            if (PreviewMediaFiles.Count == 0 && string.IsNullOrWhiteSpace(msg)) return;

            msg = msg.Trim();

            //For Api 
            var messages = new List<GeChatMessagesHttpResponse>();
            var chatMessages = new List<GeChatMessagesHttpResponse>();

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
                chatMessages.Add(textMessage);
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

                chatMessages.Add(FilesMessage);
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

                chatMessages.Add(FilesMessage);
            }

            // Raise event to update UI immediately
            ChatEvent.RaiseMessageSend(chatMessages);

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
                    ChatEvent.RaiseMessageFailed(response.Data.OfflineUniqueId);
                }
            }
        }


        [JSInvokable]
        public async Task HandleEnterKey(string message)
        {
            await NotifyLocalServer(message);
            await InvokeAsync(StateHasChanged);
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
                SentrySdk.CaptureException(ex);
            }
            finally
            {
                {
                    isCompressingMedia = false;
                }

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

        public void Dispose()
        {
            ChatEvent.OnChatSelected -= HandleChatSelected;

            foreach (var file in PreviewMediaFiles)
            {
                JS.InvokeVoidAsync("myInterop.revokePreviewUrl", file.PreviewUrl);
            }
        }
    }
}
