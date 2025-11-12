using FBMMultiMessenger.Contracts.Contracts.Account;

namespace FBMMultiMessenger.Helpers
{
    public static class DefaultMessageHelper
    {
        public static List<GetMyAccountsHttpResponse> AllAccounts { get; set; } = new List<GetMyAccountsHttpResponse>();

        public static List<GetMyAccountsHttpResponse> AccountsUsedForDefaultMessages { get; set; } = new List<GetMyAccountsHttpResponse>();

        public static List<GetMyAccountsHttpResponse> AccountsNotUsedForDefaultMessages { get; set; } = new List<GetMyAccountsHttpResponse>();

        public static List<GetMyAccountsHttpResponse> SelectableAccounts { get; set; } = new List<GetMyAccountsHttpResponse>();
    }

    public class DefaultMessageAccount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsTaken { get; set; }
    }
}
