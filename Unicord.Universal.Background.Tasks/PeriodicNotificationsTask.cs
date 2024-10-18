using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Shared;
using Windows.ApplicationModel.Background;
using Windows.Security.Credentials;
using Windows.UI.Notifications;

namespace Unicord.Universal.Background.Tasks
{
    public sealed class PeriodicNotificationsTask : IBackgroundTask
    {
        private DiscordClient _client;
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            try
            {
                if (!TryGetToken(out string token))
                    return;

                _client = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.User });
                _client.Ready += OnReady;
                _client.SocketErrored += OnSocketErrored;
                _client.SocketClosed += OnSocketClosed;

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
            catch
            {
                _deferral.Complete();
            }
        }

        private Task OnSocketClosed(DiscordClient sender, SocketCloseEventArgs args)
        {
            _deferral.Complete();
            return Task.CompletedTask;
        }

        private Task OnSocketErrored(DiscordClient sender, SocketErrorEventArgs args)
        {
            _deferral.Complete();
            return Task.CompletedTask;
        }

        private async Task OnReady(DiscordClient sender, ReadyEventArgs args)
        {
            var badgeManager = new BadgeManager(_client);
            badgeManager.Update();

            var tileManager = TileUpdateManager.CreateTileUpdaterForApplication("App");
            tileManager.EnableNotificationQueue(true);
            tileManager.Clear();

            var secondaryTileManager = new SecondaryTileManager(_client);
            //var toastManager = new ToastManager();

            var list = new List<DiscordMessage>();
            await foreach (var mention in TileManager.FetchUnreadMessages(_client))
                list.Add(mention);

            foreach (var item in list.OrderBy(d => d.CreationTimestamp))
            {
                var tileContent = NotificationUtils.CreateTileNotificationForMessage(_client, item);
                tileManager.Update(tileContent);
            }

            await sender.DisconnectAsync();
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
