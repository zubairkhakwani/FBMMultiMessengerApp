
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
}
