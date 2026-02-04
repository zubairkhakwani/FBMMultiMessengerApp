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
using Uri = Android.Net.Uri;

namespace FBMMultiMessenger.Platforms.Android
{
    [Activity(
         Theme = "@style/Maui.SplashTheme",
         MainLauncher = true,
         LaunchMode = LaunchMode.SingleInstance,
         Exported = true,
         ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

    // Deep link intent filter - this will tell that our app handles "myapp" deep links
    [IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "myapp")]


    // Standard CSV MIME type
    [IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "text/csv")]



    //Comma-separated values alternative
    [IntentFilter(
   new[] { Intent.ActionSend },
   Categories = new[] { Intent.CategoryDefault },
   DataMimeType = "text/comma-separated-values")]

    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            HandleIntent(Intent);

            WebViewSoftInputPatch.Initialize();

            OneSignal.Notifications.Clicked += HandleNotificationClicked;
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
            IntentDataHelper.IsFromNewIntent = true;
        }

        private void HandleNotificationClicked(object sender, NotificationClickedEventArgs e)
        {
            var data = e.Notification.AdditionalData;

            var isChatIdPresent = data.TryGetValue("chatId", out var chatId);
            data.TryGetValue("isSubscriptionExpired", out var subscriptionExpiredObj);
            data.TryGetValue("isSubscriptionApproved", out var subscriptionStatusObj);

            data.TryGetValue("message", out var message);

            var additionalData = new NotificationAdditionalData();

            // ChatId is only included for seller–buyer chat notifications.
            // It is not present for system notifications (e.g., subscription approval/rejection).
            if (isChatIdPresent)
            {
                bool.TryParse(subscriptionExpiredObj!.ToString(), out bool isSubscriptionExpired);

                additionalData.ChatId = Convert.ToInt32(chatId);
                additionalData.IsSubscriptionExpired = isSubscriptionExpired;
                additionalData.Message = message?.ToString() ?? string.Empty;
            }

            // If the subscription is rejected, we mark it as expired so the user is redirected to the pricing page.
            var isParsed = bool.TryParse(subscriptionStatusObj?.ToString(), out bool isSubscriptionApproved);

            if (isParsed)
            {
                additionalData.IsSubscriptionExpired = !isSubscriptionApproved;
            }

            BlazorMauiCommunicator.NotificationArrived(additionalData);
        }

        private async void HandleIntent(Intent? intent)
        {
            if (intent == null) return;

            // Handle notification deep link 
            if (intent.Data != null)
            {
                var deepLink = intent.Data.ToString() ?? string.Empty;
                IntentDataHelper.DeepLink = deepLink;
            }

            // Handle CSV file
            if (intent.Action == Intent.ActionSend)
            {
                if (intent.GetParcelableExtra(Intent.ExtraStream) is Uri uri)
                {
                    var csvBytes = await ReadCsvBytesFromUri(uri);
                    IntentDataHelper.CsvBytes = csvBytes;
                    BlazorMauiCommunicator.FileShared();
                }
            }
        }


        private async Task<byte[]> ReadCsvBytesFromUri(Uri uri)
        {
            try
            {
                using var stream = ContentResolver.OpenInputStream(uri);
                if (stream == null)
                    return Array.Empty<byte>();

                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading CSV as bytes: " + ex);
                return Array.Empty<byte>();
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
