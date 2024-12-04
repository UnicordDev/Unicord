using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Shared;
using Windows.ApplicationModel.Background;
using Windows.Security.Credentials;

namespace Unicord.Universal.Background.Tasks
{
    public sealed class RealtimeNotificationsTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        private DiscordClient _discord;
        private BadgeManager _badgeManager;
        private TileManager _tileManager;
        private SecondaryTileManager _secondaryTileManager;
        private ToastManager _toastManager;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            try
            {
                if (!TryGetToken(out string token))
                {
                    _deferral.Complete();
                    return;
                }

                _discord = new DiscordClient(new DiscordConfiguration()
                {
                    TokenType = TokenType.User,
                    Token = token,
                    MessageCacheSize = 16,
                    ReconnectIndefinitely = true
                });

                _badgeManager = new BadgeManager(_discord);
                _tileManager = new TileManager(_discord);
                _secondaryTileManager = new SecondaryTileManager(_discord);
                _toastManager = new ToastManager();

                _discord.Ready += OnReady;
                _discord.Resumed += OnResumed;
                _discord.MessageCreated += OnDiscordMessage;
                _discord.MessageUpdated += OnMessageUpdated;
                _discord.MessageAcknowledged += OnMessageAcknowledged;

                await _discord.ConnectAsync(status: UserStatus.Invisible, idlesince: DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _deferral.Complete();
            }
        }

        private async Task OnReady(DiscordClient client, ReadyEventArgs e)
        {
            await _tileManager.InitialiseAsync();
            _badgeManager.Update();

            _ = Task.Run(GCTask);
        }

        private Task OnResumed(DiscordClient sender, ResumedEventArgs args)
        {
            _ = Task.Run(GCTask);
            return Task.CompletedTask;
        }

        private async Task GCTask()
        {
            await Task.Delay(5000);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        private async Task OnDiscordMessage(DiscordClient client, MessageCreateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(client, e.Message))
                {
                    _toastManager?.HandleMessage(client, e.Message, false);
                    _badgeManager?.Update();

                    if (_tileManager != null)
                        await _tileManager.HandleMessageAsync(e.Message);
                }

                if (_secondaryTileManager != null)
                    await _secondaryTileManager.HandleMessageAsync(client, e.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(client, e.Message))
                {
                    _toastManager?.HandleMessageUpdated(client, e.Message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return Task.CompletedTask;
        }

        private async Task OnMessageAcknowledged(DiscordClient client, MessageAcknowledgeEventArgs e)
        {
            try
            {
                _badgeManager?.Update();
                _toastManager?.HandleAcknowledge(e.Channel);

                if (_tileManager != null)
                    await _tileManager.HandleAcknowledgeAsync(e.Channel);

                if (_secondaryTileManager != null)
                    await _secondaryTileManager.HandleAcknowledgeAsync(e.Channel);
            }
            catch (Exception ex)
            {
                // TODO: log
                Debug.WriteLine(ex);
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
