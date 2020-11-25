using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using Windows.UI.Notifications;

namespace Unicord.Universal.Background
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

        public void HandleMessage(DiscordMessage message)
        {
            var notification = Tools.GetWindows10Toast(message);
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
