namespace FBMMultiMessenger.Helpers
{
    public static class UserHelper
    {
        public static string Name { get; set; } = string.Empty;
        public static string Email { get; set; } = string.Empty;
        public static string ContactNumber { get; set; } = string.Empty;

        public static string StartedAt { get; set; } = string.Empty;
        public static string ExpiredAt { get; set; } = string.Empty;

        public static string RemainingTimeText { get; set; } = string.Empty;
        public static int RemainingDaysCount { get; set; }

        public static bool IsCurrentTrialSubscription { get; set; }
        public static bool HasActiveSubscription { get; set; }
        public static DateTime JoinedAt { get; set; }

        public static string Avatar => GetShortName(Name);

        private static string GetShortName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "U";

            var splittedName = fullName.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);

            if (splittedName.Length == 1)
                return splittedName[0][0].ToString().ToUpper();

            var firstLetter = splittedName[0][0];
            var secondLetter = splittedName[1][0];

            return $"{char.ToUpper(firstLetter)}{char.ToUpper(secondLetter)}";
        }
    }

}
