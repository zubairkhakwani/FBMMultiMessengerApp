namespace FBMMultiMessenger.Helpers
{
    public static class UserHelper
    {
        public static string GetShortName(string fullName)
        {
            var splitedName = fullName.Split(" ");

            var avatar = fullName[0].ToString();

            if (splitedName.Length > 1)
            {
                var firstLetter = splitedName[0][0];
                var secondLetter = splitedName[1][0];

                avatar = $"{firstLetter}{secondLetter}";
            }

            return avatar;
        }
    }
}
