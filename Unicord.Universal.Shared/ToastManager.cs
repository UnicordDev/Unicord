using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;
using Windows.UI.Notifications;

namespace Unicord.Universal.Shared
{
    public class ToastManager
    {
        private ToastNotifier _toastNotifier = null;
        private ToastNotificationHistory _toastHistory;

        public ToastManager()
        {
            _toastNotifier = ToastNotificationManager.CreateToastNotifier("App");
            _toastHistory = ToastNotificationManager.History;
        }

        public void HandleMessage(DiscordMessage message)
        {
            var notification = NotificationUtils.CreateToastNotificationForMessage(message);
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
