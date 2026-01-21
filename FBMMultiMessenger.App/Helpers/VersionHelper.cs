namespace FBMMultiMessenger.Helpers
{
    public class VersionHelper
    {
        public static Version CurrentVersion => AppInfo.Current.Version;
        public static bool IsUpdateAvailable { get; set; } = false;

        // Path to the latest  locally downloaded APK/EXE file
        public static string DownloadPath { get; set; } = string.Empty;

        // Latest version string fetched from GitHub repo
        public static string LatestVersion { get; set; } = string.Empty;

    }
}
