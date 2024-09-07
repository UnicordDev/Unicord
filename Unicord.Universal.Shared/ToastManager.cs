using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
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

        public void HandleMessage(DiscordClient client, DiscordMessage message, bool isSuppressed)
        {
            var notification = NotificationUtils.CreateToastNotificationForMessage(client, message, isSuppressed);
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
