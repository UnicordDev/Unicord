using Microsoft.Toolkit.Uwp.Helpers;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class NotificationsSettingsModel : ViewModelBase
    {
        private bool isPageEnabled = ApiInformation.IsApiContractPresent(typeof(FullTrustAppContract).FullName, 1);
        private static readonly bool _isWindows11 = SystemInformation.Instance.OperatingSystemVersion.Build >= 22000;
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse("NotificationsSettingsPage");

        public bool IsPageEnabled
        {
            get => isPageEnabled;
            set
            {
                OnPropertySet(ref isPageEnabled, value);
                InvokePropertyChanged(nameof(IsPageAndNotificationsEnabled));
            }
        }

        public bool EnableNotifications
        {
            get => App.RoamingSettings.Read(ENABLE_NOTIFICATIONS, ENABLE_NOTIFICATIONS_DEFAULT);
            set
            {
                App.RoamingSettings.Save(ENABLE_NOTIFICATIONS, value);
                InvokePropertyChanged(nameof(IsPageAndNotificationsEnabled));
            }
        }

        public bool IsPageAndNotificationsEnabled
            => IsPageEnabled && EnableNotifications;


        public bool EnableDesktopNotifications
        {
            get => App.RoamingSettings.Read(ENABLE_DESKTOP_NOTIFICAITONS, ENABLE_DESKTOP_NOTIFICAITONS_DEFAULT);
            set => App.RoamingSettings.Save(ENABLE_DESKTOP_NOTIFICAITONS, value);
        }

        public bool EnableBadgeCount
        {
            get => App.RoamingSettings.Read(ENABLE_BADGE_COUNT, ENABLE_BADGE_COUNT_DEFAULT);
            set => App.RoamingSettings.Save(ENABLE_BADGE_COUNT, value);
        }

        public bool EnableBadgeUnread
        {
            get => App.RoamingSettings.Read(ENABLE_BADGE_UNREAD, ENABLE_BADGE_UNREAD_DEFAULT);
            set => App.RoamingSettings.Save(ENABLE_BADGE_UNREAD, value);
        }
        public bool EnableLiveTiles
        {
            get => App.RoamingSettings.Read(ENABLE_LIVE_TILES, ENABLE_LIVE_TILES_DEFAULT);
            set => App.RoamingSettings.Save(ENABLE_LIVE_TILES, value);
        }

        public string EnableNotificationsDescription
            => _isWindows11 
                ? _resourceLoader.GetString("EnableNotificationsDescriptionWin11")
                : _resourceLoader.GetString("EnableNotificationsDescriptionWin10");

        public string EnableDesktopNotificationsDescription
            => _isWindows11
                ? _resourceLoader.GetString("EnableDesktopNotificationsDescriptionWin11")
                : _resourceLoader.GetString("EnableDesktopNotificationsDescriptionWin10");

        public bool IsLiveTilesVisible
            => !_isWindows11;
    }
}
