using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Calls;
using Windows.Foundation.Collections;

namespace Unicord.Universal.Voice
{
    public sealed class ServiceBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        private AppServiceConnection _connection;
        private DiscordClient _discord;
        private VoiceNextExtension _voiceNext;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance.TriggerDetails is AppServiceTriggerDetails details)
            {
                _deferral = taskInstance.GetDeferral();
                _connection = details.AppServiceConnection;
                _connection.RequestReceived += _connection_RequestReceived;

                taskInstance.Canceled += TaskInstance_Canceled;
            }
        }

        private async void _connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                if (args.Request.Message.TryGetValue("request", out var request) && request is string req)
                {
                    switch (req)
                    {
                        case "connect":
                            await ConnectAsync(sender, args, deferral);
                            return;
                        default:
                            break;
                    }
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async Task ConnectAsync(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args, AppServiceDeferral deferral)
        {
            try
            {
                var token = args.Request.Message["token"] as string;
                var server = ulong.Parse(args.Request.Message["server"] as string);
                var channelId = ulong.Parse(args.Request.Message["channel"] as string);

                async Task OnReady(ReadyEventArgs e)
                {
                    try
                    {
                        var channel = await _discord.GetChannelAsync(channelId);
                        var coordinator = VoipCallCoordinator.GetDefault();
                        await coordinator.ReserveCallResourcesAsync("Unicord.Universal.Voice.VoiceBackgroundTask");

                        var call = coordinator.RequestNewOutgoingCall(channelId.ToString(), channel.Name, "Unicord", VoipPhoneCallMedia.Audio);

                        var set = new ValueSet { ["connected"] = true };
                        await args.Request.SendResponseAsync(set);
                        deferral.Complete();

                        await Task.Delay(10000);

                        call.NotifyCallEnded();
                    }
                    catch (Exception ex)
                    {
                        var set = new ValueSet { ["connected"] = false, ["error"] = ex.Message };
                        await args.Request.SendResponseAsync(set);
                        deferral.Complete();
                    }

                }

                _discord = new DiscordClient(new DiscordConfiguration()
                {
                    Token = token,
                    TokenType = TokenType.User,
                    ReconnectIndefinitely = false,
                    AutomaticGuildSync = false,
                    MessageCacheSize = 0,
                    LightMode = true
                });

                _discord.Ready += OnReady;
                _voiceNext = _discord.UseVoiceNext();

                await _discord.ConnectAsync(status: UserStatus.Offline);
            }
            catch (Exception ex)
            {
                await args.Request.SendResponseAsync(new ValueSet() { ["connected"] = false, ["error"] = ex.Message });
                deferral.Complete();
            }
        }

        private async void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            foreach (var item in _voiceNext.ActiveConnections)
            {
                item.Value.Disconnect();
            }

            await _discord.DisconnectAsync();
            _deferral.Complete();
        }
    }
}
