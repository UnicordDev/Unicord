using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using Windows.UI.Notifications;

namespace Unicord.Universal.Shared
{
    internal class ToastManager
    {
        private ToastNotifier _toastNotifier = null;
        private ToastNotificationHistory _toastHistory;

        public ToastManager()
        {
            _toastNotifier = ToastNotificationManager.CreateToastNotifier("App");
            _toastHistory = ToastNotificationManager.History;
        }

        public void HandleMessage(DiscordClient client, DiscordMessage message, bool isSuppressed)
        {
            var notification = NotificationUtils.CreateToastNotificationForMessage(client, message, isSuppressed);
            _toastNotifier.Show(notification);
        }

        public void HandleMessageUpdated(DiscordClient client, DiscordMessage message)
        {
            var existingToast = _toastHistory.GetHistory().FirstOrDefault(t => t.Tag == message.Id.ToString());
            if (existingToast == null) return;

            var notification = NotificationUtils.CreateToastNotificationForMessage(client, message, true);
            _toastNotifier.Show(notification);
        }

        public void HandleAcknowledge(DiscordChannel channel)
        {
            if (channel == null)
                return;

            _toastHistory.RemoveGroup(channel.Id.ToString(), "App");
        }
    }
}
