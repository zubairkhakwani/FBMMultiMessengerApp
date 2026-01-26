using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Models;

namespace FBMMultiMessenger.Services.IServices
{
    public class ChatEventDispatcherService
    {
        public event Func<ChatPanelContext, Task>? OnChatSelected;
        public event Func<List<GetMyChatsHttpResponse>, Task>? OnChatLoaded;

        public event Func<GetMyChatsHttpResponse, Task>? OnNewChatReceived;
        public event Func<HandleChatHttpResponse, Task>? OnNewChatUpdated;

        public event Func<List<GeChatMessagesHttpResponse>, Task>? OnMessageSend;
        public event Func<string, Task>? OnMessageFailed;

        public Func<int, int, Task> OnLayoutChanged;

        public int SidebarZIndex = 100;
        public int MainChatZIndex = 0;

        public void RaiseLayoutChanged(int sidebarZIndex, int mainChatZIndex)
        {
            SidebarZIndex = sidebarZIndex;
            MainChatZIndex = mainChatZIndex;

            OnLayoutChanged?.Invoke(sidebarZIndex, mainChatZIndex);
        }

        public void RaiseChatSelected(ChatPanelContext context)
        {
            OnChatSelected?.Invoke(context);
        }

        public void RaiseChatLoaded(List<GetMyChatsHttpResponse> chats)
        {
            OnChatLoaded?.Invoke(chats);
        }

        public void RaiseNewChat(GetMyChatsHttpResponse chats)
        {
            OnNewChatReceived?.Invoke(chats);
        }

        public void RaiseChatUpdated(HandleChatHttpResponse updatedChat)
        {
            OnNewChatUpdated?.Invoke(updatedChat);
        }

        public void RaiseMessageSend(List<GeChatMessagesHttpResponse> sendMessages)
        {
            OnMessageSend?.Invoke(sendMessages);
        }

        public void RaiseMessageFailed(string OfflineUniqueId)
        {
            OnMessageFailed?.Invoke(OfflineUniqueId);
        }
    }

    public class HelperStlyes
    {

    }

}
