using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FBMMultiMessenger.Helpers
{
    public static class AuthenticationHelper
    {
        public static void HandleUnAuthorizedAccess(AuthenticationState context, NavigationManager navigationManager)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                navigationManager.NavigateTo("/login");
                return;
            }

            var hasActiveSubscription = context.User.FindFirst("hasActiveSubscription")?.Value != null;
            var isSubscriptionExpired = context.User.FindFirst("isSubscriptionExpired")?.Value != null;

            if (!hasActiveSubscription || isSubscriptionExpired)
            {
                var redirectReason2 = string.Empty;

                if (isSubscriptionExpired && !hasActiveSubscription)
                {
                    redirectReason2 = "Oops! Your subscription has expired. Renew today to pick up right where you left off!";
                }
                else
                {
                    redirectReason2 = "Ready to unlock the full experience? Subscribe now to unlock powerful features and take your experience to the next level!";
                }

                navigationManager.NavigateTo($"/pricing?redirectReason={Uri.EscapeDataString(redirectReason2)}");

                return;
            }
        }
    }
}
