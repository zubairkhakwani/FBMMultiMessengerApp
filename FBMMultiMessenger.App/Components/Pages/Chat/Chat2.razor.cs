using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class Chat2 : IDisposable
    {

        [Inject]
        public ChatEventDispatcherService ChatEvent { get; set; }

        [Inject]
        public BackButtonService BackButtonService { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        protected override void OnInitialized()
        {
            BackButtonService.BackButtonPressed += HandleBackButtonPressed;
        }

        private void HandleBackButtonPressed()
        {
            var sideBarZIndex = ChatEvent.SidebarZIndex;
            var mainChatZIndex = ChatEvent.MainChatZIndex;

            if (sideBarZIndex > mainChatZIndex)
            {
                Navigation.NavigateTo("/Account");
                return;
            }

            ChatEvent.RaiseLayoutChanged(100, 0);
        }

        public void Dispose()
        {
            BackButtonService.BackButtonPressed -= HandleBackButtonPressed;
        }

    }



}