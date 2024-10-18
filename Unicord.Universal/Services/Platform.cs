using Windows.System.Profile;

namespace Unicord.Universal.Services
{
    /// <summary>
    /// This class contains helper methods for determining the current platform.
    /// It should only be used for specific behavioural changes that depend on the current device form-factor,
    /// e.g. to mark the user as idle on mobile to ensure they get push notifications to other platforms
    /// </summary>
    internal static class SystemPlatform
    {
        private static string _platform;
        static SystemPlatform()
        {
            _platform = AnalyticsInfo.VersionInfo.DeviceFamily;
        }

        public static bool Desktop
            => _platform == "Windows.Desktop";
        public static bool Mobile
            => _platform == "Windows.Mobile";
        public static bool Xbox
            => _platform == "Windows.Xbox";
    }
}
