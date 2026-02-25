
namespace FBMMultiMessenger.Contracts.Enums
{
    public enum PaymentStatus
    {
        Approved = 1,
        Pending = 2,
        Rejected = 3
    }
    public enum BillingCylce
    {
        Monthly,
        SemiAnnual,
        Annual
    }
    public enum AccountAuthStatus
    {
        All = 0,
        Idle = 1,
        LoggedIn = 2,
        LoggedOut = 3,
    }

    public enum NotificationCategory
    {
        Chat = 1,
        Subscription = 2,
        Account = 3,
        System = 4
    }

    public enum MessageReplyType
    {
        Text = 1,
        Image = 2,
        Video = 3,
        Audio = 4
    }
}
