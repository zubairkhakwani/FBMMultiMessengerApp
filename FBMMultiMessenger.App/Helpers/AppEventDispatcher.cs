namespace FBMMultiMessenger.Helpers
{
    internal static class AppEventDispatcher
    {
        public static event Func<Task> OnProfleChanged;

        public static void ProfileChanged()
        {
            OnProfleChanged?.Invoke();
        }
    }
}
