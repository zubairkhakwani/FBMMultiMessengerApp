using FBMMultiMessenger.Services.IServices;
using System.Diagnostics;

namespace FBMMultiMessenger.Services
{
    public class FacebookService : IFacebookService
    {
        public async Task OpenLink(string link)
        {
#if ANDROID
            await OpenProfileAndriod(link);
#elif WINDOWS
            await OpenProfileWindows(link);
#endif
        }

        private async Task OpenProfileAndriod(string link)
        {
#if ANDROID
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var activity = Platform.CurrentActivity;
                if (activity == null) return;

                var dialog = new Android.App.Dialog(activity, Android.Resource.Style.ThemeNoTitleBarFullScreen);
                var rootLayout = new Android.Widget.FrameLayout(activity);

                var webView = new Android.Webkit.WebView(activity);

                // Configure WebView
                var settings = webView.Settings;
                settings.JavaScriptEnabled = true;
                settings.DomStorageEnabled = true;
                settings.SetSupportMultipleWindows(false);

                // Use custom WebViewClient
                webView.SetWebViewClient(new FacebookWebViewClient());

                webView.LoadUrl(link);

                // Close button
                var closeButton = new Android.Widget.Button(activity) { Text = "✕" };
                var closeParams = new Android.Widget.FrameLayout.LayoutParams(120, 120)
                {
                    Gravity = Android.Views.GravityFlags.Top | Android.Views.GravityFlags.End
                };
                closeParams.SetMargins(20, 40, 20, 20);
                closeButton.LayoutParameters = closeParams;
                closeButton.Click += (s, e) => dialog.Dismiss();

                rootLayout.AddView(webView);
                rootLayout.AddView(closeButton);
                dialog.SetContentView(rootLayout);
                dialog.Show();
            });
#endif
        }


        private async Task OpenProfileWindows(string link)
        {

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var url = link;

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



        // Custom WebViewClient
#if ANDROID
        public class FacebookWebViewClient : Android.Webkit.WebViewClient
        {
            public override bool ShouldOverrideUrlLoading(
                Android.Webkit.WebView view,
                Android.Webkit.IWebResourceRequest request)
            {
                var url = request.Url.ToString();

                // Handle normal web URLs (http/https)
                if (url.StartsWith("http://") || url.StartsWith("https://"))
                {
                    // Keep Facebook URLs in WebView
                    if (url.Contains("facebook.com") || url.Contains("fb.com"))
                    {
                        view.LoadUrl(url);
                        return true;
                    }

                    // For other web URLs, you can decide
                    view.LoadUrl(url);
                    return true;
                }

                // Handle special URL schemes (fb://, intent://, tel:, etc.)
                try
                {
                    var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
                    intent.SetData(Android.Net.Uri.Parse(url));

                    // Check if any app can handle this URL
                    var activity = Platform.CurrentActivity;
                    if (activity != null &&
                        intent.ResolveActivity(activity.PackageManager) != null)
                    {
                        activity.StartActivity(intent);
                    }
                }
                catch (Exception ex)
                {
                    // If it fails, just ignore it
                }

                return true; // We handled it (or tried to)
            }

            // Handle errors
            public override void OnReceivedError(
                Android.Webkit.WebView view,
                Android.Webkit.IWebResourceRequest request,
                Android.Webkit.WebResourceError error)
            {
                base.OnReceivedError(view, request, error);
            }
        }
#endif
    }
}

