namespace FBMMultiMessenger.Helpers
{
    public static class PlatformHelper
    {
        public static readonly bool IsMobilePlatform = DeviceInfo.Platform == DevicePlatform.WinUI;
    }
}
