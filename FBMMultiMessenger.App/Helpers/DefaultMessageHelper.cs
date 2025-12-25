using FBMMultiMessenger.Contracts.Contracts.Account;

namespace FBMMultiMessenger.Helpers
{
    public static class DefaultMessageHelper
    {
        public static List<UserAccountsHttpResponse> AllAccounts { get; set; } = new List<UserAccountsHttpResponse>();

        public static List<UserAccountsHttpResponse> AccountsUsedForDefaultMessages { get; set; } = new List<UserAccountsHttpResponse>();

        public static List<UserAccountsHttpResponse> AccountsNotUsedForDefaultMessages { get; set; } = new List<UserAccountsHttpResponse>();

        public static List<UserAccountsHttpResponse> SelectableAccounts { get; set; } = new List<UserAccountsHttpResponse>();
    }

    public class DefaultMessageAccount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsTaken { get; set; }
    }
}
