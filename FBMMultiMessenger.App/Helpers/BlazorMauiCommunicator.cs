using FBMMultiMessenger.Models;


namespace FBMMultiMessenger.Helpers
{
    internal static class BlazorMauiCommunicator
    {
        public static event Func<NotificationAdditionalData, Task> OnNotificationClicked;
        public static event Func<Task> OnFileShared;

        public static void NotificationArrived(NotificationAdditionalData data)
        {
            OnNotificationClicked?.Invoke(data);
        }

        public static void FileShared()
        {
            OnFileShared?.Invoke();
        }
    }
}
