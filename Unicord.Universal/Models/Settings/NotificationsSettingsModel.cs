using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class NotificationsSettingsModel : ViewModelBase
    {
        private bool isPageEnabled = ApiInformation.IsApiContractPresent(typeof(FullTrustAppContract).FullName, 1);

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
    }
}
