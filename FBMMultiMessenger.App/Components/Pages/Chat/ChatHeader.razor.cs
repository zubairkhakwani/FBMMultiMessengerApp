using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class ChatHeader
    {
        public ChatHeaderContext Context { get; set; } = new();
        public string? SelectedFbChatId { get; set; }


        [Inject]
        public ChatEventDispatcherService ChatEvent { get; set; }

        protected override void OnInitialized()
        {
            ChatEvent.OnChatSelected += HandleChatSelected;
        }

        private async Task HandleChatSelected(ChatPanelContext context)
        {
            Context = context.HeaderContext;
            SelectedFbChatId = context.SelectedFbChatId;

            await InvokeAsync(StateHasChanged);
        }

        private void ShowSideBarView()
        {
            ChatEvent.RaiseLayoutChanged(100, 0);
        }

        private void HandleChatMenuOpen()
        {
            //ShowChatMenuAction = true;
        }

        private void HandleChatMenuClose()
        {
            //ShowChatMenuAction = false;
        }

    }
}
