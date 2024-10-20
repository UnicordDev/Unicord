using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Models.Messaging;
using Windows.ApplicationModel.Core;
using Windows.Security.Credentials;
using Windows.UI.Core;

namespace Unicord.Universal.Services
{
    internal class DiscordManager
    {
        private static DiscordClient _discord;
        private static ILogger<DiscordManager> _logger
            = Logger.GetLogger<DiscordManager>();

        private static readonly SemaphoreSlim _connectSemaphore
            = new SemaphoreSlim(1);
        private static TaskCompletionSource<ReadyEventArgs> _readySource
            = new TaskCompletionSource<ReadyEventArgs>();

        public static DiscordClient Discord => _discord;

        public static void KickoffConnectionAsync()
        {
            _ = Task.Run(async () =>
            {
                if (LoginService.TryGetToken(out var token))
                    await ConnectAsync(token);
            });
        }

        public static async Task WaitForReadyAsync()
            => await _readySource.Task;

        public static async Task ConnectAsync(
            string token,
            UserStatus status = UserStatus.Online)
        {
            await _connectSemaphore.WaitAsync();
            try
            {
                if (Discord != null)
                    return;

                _readySource = new TaskCompletionSource<ReadyEventArgs>();

                try
                {
                    Task ReadyHandler(DiscordClient sender, ReadyEventArgs e)
                    {
                        // TODO: find a way to save this more securely, the background process can't retrieve from the credential locker?
                        App.LocalSettings.Save("Token", token);

                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;
                        _readySource.TrySetResult(e);
                        return Task.CompletedTask;
                    }

                    Task SocketErrored(DiscordClient sender, SocketErrorEventArgs e)
                    {
                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;
                        _logger.LogError(e.Exception, "Socket errored!");
                        _readySource.SetException(e.Exception);
                        return Task.CompletedTask;
                    }

                    Task ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
                    {
                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;

                        _logger.LogError(e.Exception, "Client errored!");

                        _readySource.SetException(e.Exception);
                        return Task.CompletedTask;
                    }

                    _discord = new DiscordClient(new DiscordConfiguration()
                    {
                        Token = token,
                        TokenType = TokenType.User,
                        LoggerFactory = Logger.LoggerFactory,
                        ReconnectIndefinitely = true
                    });

                    Discord.Ready += ReadyHandler;
                    Discord.SocketErrored += SocketErrored;
                    Discord.ClientErrored += ClientErrored;
                    Discord.CaptchaRequested += OnDiscordCaptchaRequested;
                    Discord.AuthTokenUpdate += OnDiscordTokenUpdated;

                    DiscordClientMessenger.Register(Discord);

                    await Discord.ConnectAsync(status: status,
                        idlesince: SystemPlatform.Desktop ? null : DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure when logging in!");
                    throw;
                }
            }
            finally
            {
                _connectSemaphore.Release();
            }
        }

        private static Task OnDiscordTokenUpdated(DiscordClient sender, AuthTokenUpdatedEventArgs args)
        {
            var vault = new PasswordVault();
            try
            {
                foreach (var c in vault.FindAllByResource(Constants.TOKEN_IDENTIFIER))
                    vault.Remove(c);
            }
            catch { }

            var newToken = new PasswordCredential(Constants.TOKEN_IDENTIFIER, "Default", args.Token);
            vault.Add(newToken);

            // ditto above about the background process
            App.LocalSettings.Save("Token", args.Token);

            return Task.CompletedTask;
        }

        private static async Task OnDiscordCaptchaRequested(BaseDiscordClient sender, CaptchaRequestEventArgs args)
        {
            var tcs = new TaskCompletionSource<DiscordCaptchaResponse>();

            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                var dialog = new CaptchaRequestDialog(args.Request);
                await dialog.ShowAsync();
                tcs.SetResult(dialog.CaptchaResponse);
            });

            args.SetResponse(await tcs.Task);
        }

        internal static async Task LogoutAsync()
        {
            if (Discord == null) return;

            var discord = _discord;
            try
            {
                DiscordClientMessenger.Unregister(discord);
                await discord.DisconnectAsync();
                discord.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when disposing of DiscordClient!");
            }
            finally
            {
                _discord = null;
            }
        }
    }
}
