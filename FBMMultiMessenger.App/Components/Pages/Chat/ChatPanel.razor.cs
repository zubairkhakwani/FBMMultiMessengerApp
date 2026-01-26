using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class ChatPanel : IDisposable
    {
        private int MainChatZIndex = 0;

        [Inject]
        public ChatEventDispatcherService ChatEvent { get; set; }

        [Inject]
        private BackButtonService BackButtonService { get; set; }

        protected override void OnInitialized()
        {
            ChatEvent.OnLayoutChanged += ShowMainChatView;
            BackButtonService.BackButtonPressed += HandleBackButtonPressed;
        }
        private async Task ShowMainChatView(int sidebarZIndex, int mainChatZIndex)
        {
            MainChatZIndex = mainChatZIndex;
            await InvokeAsync(StateHasChanged);
        }

        private void HandleBackButtonPressed()
        {
            if (MainChatZIndex > 0)
            {
                MainChatZIndex = 0;
                InvokeAsync(StateHasChanged);
            }
        }
        public void Dispose()
        {
            ChatEvent.OnLayoutChanged += ShowMainChatView;
            BackButtonService.BackButtonPressed -= HandleBackButtonPressed;
        }
    }
}
