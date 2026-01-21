namespace FBMMultiMessenger.Models
{
    // Represents the version information of the application.
    public class AppVersion
    {
        public string DownloadedVersion { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastCheckedAt { get; set; }
    }


    // Represents the version information retrieved from github repo.
    public class RemoteVersion
    {
        public Platform Android { get; set; } = new Platform();
        public Platform Desktop { get; set; } = new Platform();
    }

    public class Platform
    {
        public string LatestVersion { get; set; } = string.Empty;
        public string AppUrl { get; set; } = string.Empty;
    }
}
