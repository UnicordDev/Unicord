using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Unicord.Universal.Shared;
using Windows.ApplicationModel.Background;
using Windows.Security.Credentials;
using Windows.UI.Notifications;

namespace Unicord.Universal.Background.Tasks
{
    public sealed class PeriodicNotificationsTask : IBackgroundTask
    {
        private readonly ILogger<PeriodicNotificationsTask> _logger
            = Logger.GetLogger<PeriodicNotificationsTask>();

        private DiscordClient _client;
        private BackgroundTaskDeferral _deferral;

        private long _startTimestamp;
        private TimeSpan Runtime 
            => TimeSpan.FromSeconds((double)(Stopwatch.GetTimestamp() - _startTimestamp) / Stopwatch.Frequency);

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _logger.LogInformation("Starting periodic notification task...");
            _startTimestamp = Stopwatch.GetTimestamp();
            _deferral = taskInstance.GetDeferral();
            try
            {
                if (!TryGetToken(out string token))
                    return;

                _client = new DiscordClient(new DiscordConfiguration()
                {
                    Token = token,
                    TokenType = TokenType.User,
                    AutoReconnect = false,
                    MessageCacheSize = 128,
                    LoggerFactory = Logger.LoggerFactory
                });
                _client.Ready += OnReady;
                _client.SocketErrored += OnSocketErrored;
                _client.SocketClosed += OnSocketClosed;

                await _client.ConnectAsync(status: UserStatus.Invisible);
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
            _logger.LogInformation("Socket errored! Failed in {Time}!", Runtime);
            _deferral.Complete();

            return Task.CompletedTask;
        }

        private async Task OnReady(DiscordClient sender, ReadyEventArgs args)
        {
            _logger.LogInformation("Got ready, updating tiles, badge...");
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

            _logger.LogInformation("All done! Finished in {Time}!", Runtime);
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
