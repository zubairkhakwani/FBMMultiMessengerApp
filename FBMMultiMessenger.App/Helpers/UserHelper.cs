namespace FBMMultiMessenger.Helpers
{
    public static class UserHelper
    {

        public static string GetShortName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return string.Empty;
            }

            var shortName = string.Join("", fullName.Split(" ").Select(x => x[0]).ToList());

            return shortName;
        }
    }
}
