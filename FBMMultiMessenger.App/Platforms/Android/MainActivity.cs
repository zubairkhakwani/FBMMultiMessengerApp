using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using FBMMultiMessenger.Services;

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
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null)
            {
                HandleIntent(intent);
            }
        }

        private void HandleIntent(Intent? intent)
        {
            if (intent?.Data != null)
            {
                var deepLink = intent.Data.ToString() ?? string.Empty;
                var uri = new Uri(deepLink);
                var route = $"/{uri.Host}{uri.Query}";
                Preferences.Set("PendingDeepLink", route);
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
