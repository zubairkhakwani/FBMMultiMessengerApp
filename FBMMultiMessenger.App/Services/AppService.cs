using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using System.Diagnostics;
using System.Text.Json;

namespace FBMMultiMessenger.Services
{
    public class AppService : IAppService
    {
        private string _downloadFolder;
        private string _versionFilePath;
        public AppService()
        {
            this._downloadFolder = GetDownloadPath();
            this._versionFilePath = Path.Combine(_downloadFolder, "version.json");
        }
        public async Task CheckForUpdateAsync()
        {
            try
            {
                var client = new HttpClient();

                string jsonBase64 = await client.GetStringAsync("https://github.com/user-attachments/files/24979312/version.json");

                var remoteVersion = JsonSerializer.Deserialize<RemoteVersion>(jsonBase64, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (remoteVersion is null) return;

                var currentApp = await LoadVersionAsync();
                var latestVersionString = PlatformHelper.IsMobilePlatform ? remoteVersion.Android.LatestVersion : remoteVersion.Desktop.LatestVersion;


                var downloadUrl = PlatformHelper.IsMobilePlatform ? remoteVersion.Android.AppUrl : remoteVersion.Desktop.AppUrl;

                var latestVer = new Version(latestVersionString);
                var currentVer = new Version(VersionHelper.CurrentVersion);

                string fileName = Path.GetFileName(downloadUrl);

                var downloadPath = Path.Combine(_downloadFolder, fileName);

                if (latestVer > currentVer)
                {
                    bool needsDownload = currentApp.DownloadedVersion != latestVersionString;

                    if (needsDownload)
                    {
                        if (File.Exists(downloadPath))
                        {
                            File.Delete(downloadPath);
                        }

                        var data = await client.GetByteArrayAsync(downloadUrl);
                        await File.WriteAllBytesAsync(downloadPath, data);

                        currentApp.DownloadedVersion = latestVersionString;
                        await SaveVersionAsync(currentApp);
                    }

                    VersionHelper.DownloadPath = downloadPath;
                    VersionHelper.LatestVersion = latestVersionString;
                    VersionHelper.IsUpdateAvailable = true;

                }
                else
                {
                    // Clean up
                    if (File.Exists(downloadPath))
                    {
                        File.Delete(downloadPath);
                    }

                    currentApp.DownloadedVersion = string.Empty;
                    await SaveVersionAsync(currentApp);
                }

                await UpdateLastCheckedAsync();
            }
            catch (Exception ex)
            {
            }
        }

        private string GetDownloadPath()
        {

#if ANDROID
            // Android: Use app's external cache directory (doesn't require WRITE_EXTERNAL_STORAGE permission)
            var context = Android.App.Application.Context;
            var externalCacheDir = context.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads);

            if (externalCacheDir != null)
            {
                return externalCacheDir.AbsolutePath;
            }

            // Fallback to internal cache if external not available
            var cacheDir = context.CacheDir;
            return cacheDir?.AbsolutePath ?? string.Empty;
#else
    // Windows/Desktop: Use LocalApplicationData
    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
        }


        public async Task<AppVersion> LoadVersionAsync()
        {
            if (!File.Exists(_versionFilePath))
            {
                var defaultVersion = new AppVersion();
                await SaveVersionAsync(defaultVersion);
                return defaultVersion;
            }

            var json = await File.ReadAllTextAsync(_versionFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<AppVersion>(json) ?? new AppVersion();
        }

        public async Task SaveVersionAsync(AppVersion version)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(version, options);
            await File.WriteAllTextAsync(_versionFilePath, json);
        }

        private async Task UpdateLastCheckedAsync()
        {
            var version = await LoadVersionAsync();
            version.LastCheckedAt = DateTime.UtcNow;
            await SaveVersionAsync(version);
        }

        public async Task UpdateDesktopExe()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = VersionHelper.DownloadPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(psi);

                // Give a small delay before closing
                await Task.Delay(500);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching installer: {ex.Message}");
                // Optionally show error to user
            }
        }


        public async Task UpdateAndriodApk()
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;

                var file = new Java.IO.File(VersionHelper.DownloadPath);
                var uri = FileProvider.GetUriForFile(
                    context,
                    context.PackageName + ".fileprovider",
                    file);


                var installIntent = new Android.Content.Intent(Android.Content.Intent.ActionView);
                installIntent.SetDataAndType(uri, GetMimeType(uri.Path));
                installIntent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission | Android.Content.ActivityFlags.NewTask);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    context.StartActivity(installIntent);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating APK: {ex.Message}");
            }
#endif
        }

        static string GetMimeType(string filePath)
        {
#if ANDROID
            var extension = Android.Webkit.MimeTypeMap.GetFileExtensionFromUrl(filePath);
            var mime = Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension?.ToLower() ?? "");

            return mime ?? "*/*";
#endif

            return "";
        }

    }

}
