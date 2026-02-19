namespace FBMMultiMessenger.Helpers
{
    public class VersionHelper
    {
        public static string CurrentVersion => GetCurrentVersion();
        public static bool IsUpdateAvailable { get; set; } = false;

        // Path to the latest  locally downloaded APK/EXE file
        public static string DownloadPath { get; set; } = string.Empty;

        // Latest version string fetched from GitHub repo
        public static string LatestVersion { get; set; } = string.Empty;


        private static string GetCurrentVersion()
        {
            // Windows appends <ApplicationVersion> (the build number) as a 4th digit (e.g., 1.0.0.1).
            // We use ToString(3) to force only "Major.Minor.Build" (e.g., 1.0.0) so it matches
            // our external version checks and doesn't break comparison logic.

            return AppInfo.Current.Version.ToString(3);
        }
    }
}
