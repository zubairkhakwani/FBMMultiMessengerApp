namespace FBMMultiMessenger.Services
{
    public class BackButtonService
    {
        public event Action? BackButtonPressed;
        public bool HasSubscribers => BackButtonPressed != null;
        public void NotifyBackPressed()
        {
            BackButtonPressed?.Invoke();
        }
    }
}
