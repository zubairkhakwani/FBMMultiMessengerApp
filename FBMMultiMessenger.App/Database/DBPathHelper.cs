namespace FBMMultiMessenger.Database
{
    public static class DBPathHelper
    {
        public static string GetDbPath()
        {
            // AppDataDirectory is sandboxed per app on Android
            return Path.Combine(FileSystem.AppDataDirectory, "messenger.db");
        }
    }
}
