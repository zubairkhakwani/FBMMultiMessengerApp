using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core.Notifications;

namespace FBMMultiMessenger.Platforms.Android
{
    [Activity(
         Theme = "@style/Maui.SplashTheme",
         MainLauncher = true,
         LaunchMode = LaunchMode.SingleTop,
         Exported = true,
         ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "myapp")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            WebViewSoftInputPatch.Initialize();

            HandleIntent(Intent);

            OneSignal.Notifications.Clicked += HandleNotificationClicked;
        }

        private void HandleNotificationClicked(object sender, NotificationClickedEventArgs e)
        {
            var data = e.Notification.AdditionalData;

            var isFbChatIdPresent = data.TryGetValue("fbChatId", out var fbChatIdObj);
            data.TryGetValue("isSubscriptionExpired", out var subscriptionExpiredObj);
            data.TryGetValue("isSubscriptionApproved", out var subscriptionStatusObj);

            data.TryGetValue("message", out var message);

            var additionalData = new NotificationAdditionalData();

            // FbChatId is only included for seller–buyer chat notifications.
            // It is not present for system notifications (e.g., subscription approval/rejection).
            if (isFbChatIdPresent)
            {
                bool.TryParse(subscriptionExpiredObj!.ToString(), out bool isSubscriptionExpired);

                additionalData.FbChatId = fbChatIdObj?.ToString() ?? string.Empty;
                additionalData.IsSubscriptionExpired = isSubscriptionExpired;
                additionalData.Message = message?.ToString() ?? string.Empty;
            }

            // If the subscription is rejected, we mark it as expired so the user is redirected to the pricing page.
            var isParsed = bool.TryParse(subscriptionStatusObj!.ToString(), out bool isSubscriptionApproved);

            if (isParsed)
            {
                additionalData.IsSubscriptionExpired = !isSubscriptionApproved;
            }

            BlazorMauiCommunicator.NotificationArrived(additionalData);
        }

        private void HandleIntent(Intent? intent)
        {
            if (intent?.Data != null)
            {
                var deepLink = intent.Data.ToString() ?? string.Empty;
                Preferences.Set("PendingDeepLink", deepLink);
            }
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Keycode.Back && e.Action == KeyEventActions.Down)
            {
                var service = IPlatformApplication.Current.Services.GetService<BackButtonService>();
                if (service?.HasSubscribers == true)
                {
                    service.NotifyBackPressed();
                    return true;
                }
            }


            return base.DispatchKeyEvent(e);
        }
    }
}
