
#if ANDROID
using Android.Webkit;
#endif

using FBMMultiMessenger.Services.IServices;
using System.Diagnostics;


namespace FBMMultiMessenger.Services
{
    public class FacebookService : IFacebookService
    {
        public async Task OpenProfile(string profileId)
        {
#if ANDROID
            await OpenProfileAndroid(profileId);
#elif WINDOWS
            await OpenProfileWindows(profileId);
#endif
        }

        private async Task OpenProfileAndroid(string profileId)
        {
#if ANDROID
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var activity = Platform.CurrentActivity;
                if (activity == null) return;

                // Root layout
                var rootLayout = new Android.Widget.FrameLayout(activity);

                // WebView
                var webView = new Android.Webkit.WebView(activity);
                webView.Settings.JavaScriptEnabled = true;
                webView.Settings.DomStorageEnabled = true;
                webView.LoadUrl($"https://www.facebook.com/{profileId}");

                // Close Button
                //var closeButton = new Android.Widget.ImageButton(activity);
                //closeButton.SetImageResource(Android.Resource.Drawable.IcMenuCloseClearCancel);
                //closeButton.SetBackgroundColor(Android.Graphics.Color.Transparent);

                //var closeParams = new Android.Widget.FrameLayout.LayoutParams(
                //    Android.Widget.FrameLayout.LayoutParams.WrapContent,
                //    Android.Widget.FrameLayout.LayoutParams.WrapContent
                //);
                //closeParams.Gravity = Android.Views.GravityFlags.Top | Android.Views.GravityFlags.End;
                //closeParams.SetMargins(20, 40, 20, 20);

                //closeButton.LayoutParameters = closeParams;

                // Add views
                rootLayout.AddView(webView);
                // rootLayout.AddView(closeButton);

                // Dialog
                //var dialog = new Android.App.Dialog(activity, Android.Resource.Style.ThemeNoTitleBarFullScreen);
                //dialog.SetContentView(rootLayout);

                //closeButton.Click += (s, e) =>
                //{
                //    dialog.Dismiss(); // User returns back
                //};

                //dialog.Show();
            });
#endif
        }

#if ANDROID
        private void SetFacebookCookiesAndroid(CookieManager cookieManager)
        {
            // TODO: Replace with YOUR actual Facebook cookies
            cookieManager.SetCookie(".facebook.com", "c_user=YOUR_USER_ID");
            cookieManager.SetCookie(".facebook.com", "xs=YOUR_XS_VALUE");
            cookieManager.SetCookie(".facebook.com", "datr=YOUR_DATR_VALUE");
            cookieManager.SetCookie(".facebook.com", "fr=YOUR_FR_VALUE");

            cookieManager.Flush();
        }
#endif

        private async Task OpenProfileWindows(string profileId)
        {

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var url = $"https://www.facebook.com/{profileId}";

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    // Fallback: try different approach
                    Process.Start("cmd", $"/c start {url}");
                }
            });
        }
    }
}

