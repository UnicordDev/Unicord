using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using Unicord.Universal.Shared;
using Windows.ApplicationModel.Background;
using Windows.Security.Credentials;
using Windows.UI.Notifications;

namespace Unicord.Universal.Background.Tasks
{
    public sealed class PeriodicNotificationsTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                if (!TryGetToken(out string token))
                    return;

                var tileUpdateManager = TileUpdateManager.CreateTileUpdaterForApplication();
                var toastNotifier = ToastNotificationManager.CreateToastNotifier();

                var restClient = new DiscordRestClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.User });
                await restClient.InitializeAsync();

                var mentions = await restClient.GetUserMentionsAsync(25, true, true);

                foreach (var mention in mentions)
                {
                    if (!NotificationUtils.WillShowToast(restClient, mention))
                        continue;

                    var tileNotification = NotificationUtils.CreateTileNotificationForMessage(restClient, mention);
                    var toastNotification = NotificationUtils.CreateToastNotificationForMessage(restClient, mention);

                    tileUpdateManager.Update(tileNotification);
                    toastNotifier.Show(toastNotification);
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        internal static bool TryGetToken(out string token)
        {
            try
            {
                var passwordVault = new PasswordVault();
                var credential = passwordVault.Retrieve(Constants.TOKEN_IDENTIFIER, "Default");
                credential.RetrievePassword();

                token = credential.Password;
                return true;
            }
            catch { }

            token = null;
            return false;
        }
    }
}
