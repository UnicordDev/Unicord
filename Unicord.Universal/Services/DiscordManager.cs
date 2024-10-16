using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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

        public static DiscordClient Discord
        {
            get => _discord;
        }

        private static readonly SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1);
        private static TaskCompletionSource<ReadyEventArgs> _readySource = new TaskCompletionSource<ReadyEventArgs>();

        internal static void KickoffConnectionAsync()
        {
            _ = Task.Run(async () =>
            {
                if (TryGetToken(out var token))
                    await LoginAsync(token, null, null, true);
            });
        }

        internal static async Task LoginAsync(
            string token,
            AsyncEventHandler<DiscordClient, ReadyEventArgs> onReady,
            Func<Exception, Task> onError,
            bool background,
            UserStatus status = UserStatus.Online)
        {
            await _connectSemaphore.WaitAsync();
            try
            {
                if (Discord != null)
                {
                    try
                    {
                        var res = await _readySource.Task;
                        if (onReady != null)
                            await onReady(Discord, res);
                    }
                    catch (Exception ex)
                    {
                        if (onError != null)
                            await onError(ex);
                    }

                    return;
                }

                if (App.RoamingSettings.Read(Constants.VERIFY_LOGIN, false))
                {
                    if (background || !(await WindowsHelloManager.VerifyAsync(Constants.VERIFY_LOGIN, "VerifyLoginDisplayReason")))
                    {
                        if (onError != null)
                            await onError(null);
                        return;
                    }
                }

                try
                {
                    async Task ReadyHandler(DiscordClient sender, ReadyEventArgs e)
                    {
                        // TODO: find a way to save this more securely, the background process can't retrieve from the credential locker?
                        App.LocalSettings.Save("Token", token);
                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;
                        _readySource.TrySetResult(e);
                        if (onReady != null)
                        {
                            await onReady(sender, e);
                        }

                        onError = null;
                    }

                    Task SocketErrored(DiscordClient sender, SocketErrorEventArgs e)
                    {
                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;

                        Logger.LogError(e.Exception);

                        _readySource.SetException(e.Exception);
                        return Task.CompletedTask;
                    }

                    Task ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
                    {
                        sender.Ready -= ReadyHandler;
                        sender.SocketErrored -= SocketErrored;
                        sender.ClientErrored -= ClientErrored;

                        Logger.LogError(e.Exception);

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
                    Logger.LogError(ex);
                    Tools.ResetPasswordVault();
                    _readySource.TrySetException(ex);
                    if (onError != null)
                        await onError(ex);
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

            try
            {
                await Discord.DisconnectAsync();
                Discord.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
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
