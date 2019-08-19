using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext.Entities;
using Newtonsoft.Json;
using Unicord.Universal.Voice.Background;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Calls;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Unicord.Universal.Voice
{
    public class VoiceConnectionModel : PropertyChangedBase
    {
        private AppServiceConnection _appServiceConnection;
        private VoipCallCoordinator _voipCallCoordinator;
        private VoipPhoneCall _voipPhoneCall;
        private VoiceState _state = VoiceState.None;
        private TaskCompletionSource<VoiceStateUpdateEventArgs> _voiceStateUpdateCompletion;
        private TaskCompletionSource<VoiceServerUpdateEventArgs> _voiceServerUpdateCompletion;
        private uint _webSocketPing;
        private uint _udpPing;

        public DiscordChannel Channel { get; }

        public bool Muted
        {
            get => _state != VoiceState.None;
            set => OnPropertySet(ref _state, _state ^ VoiceState.Muted);
        }

        public bool Deafened
        {
            get => _state.HasFlag(VoiceState.Deafened);
            set
            {
                OnPropertySet(ref _state, _state ^ VoiceState.Deafened);
                InvokePropertyChanged(nameof(Muted));
            }
        }

        public uint WebSocketPing { get => _webSocketPing; set => OnPropertySet(ref _webSocketPing, value); }
        public uint UdpPing { get => _udpPing; set => OnPropertySet(ref _udpPing, value); }

        public event EventHandler<EventArgs> Disconnected;

        public VoiceConnectionModel(DiscordChannel channel)
        {
            _voiceStateUpdateCompletion = new TaskCompletionSource<VoiceStateUpdateEventArgs>();
            _voiceServerUpdateCompletion = new TaskCompletionSource<VoiceServerUpdateEventArgs>();
            _voipCallCoordinator = VoipCallCoordinator.GetDefault();

            Channel = channel;
            _appServiceConnection = new AppServiceConnection()
            {
                AppServiceName = "com.wankerr.Unicord.Voice",
                PackageFamilyName = Package.Current.Id.FamilyName
            };

            _appServiceConnection.RequestReceived += OnRequestReceived;
            _appServiceConnection.ServiceClosed += OnServiceClosed;

            this.PropertyChanged += OnPropertyChanged;
        }

        private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Muted))
            {
                await ToggleMuteAsync();
            }

            if (e.PropertyName == nameof(Deafened))
            {
                await ToggleDeafenAsync();
            }
        }

        public async Task ConnectAsync()
        {
            if (ApiInformation.IsTypePresent("Windows.ApplicationModel.Calls.VoipPhoneCallResourceReservationStatus"))
            {
                var appServiceStatus = await _appServiceConnection.OpenAsync();
                if (appServiceStatus != AppServiceConnectionStatus.Success)
                    throw new Exception("Unable to connect to AppService! " + appServiceStatus);

                var stateRequest = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.StateRequest };
                var response = await SendRequestAsync(stateRequest);
                var state = (VoiceServiceState)(uint)response["state"];

                if (state == VoiceServiceState.Connected)
                {
                    var channel_id = (ulong)response["channel_id"];
                    var guild_id = (ulong)response["guild_id"];

                    if (channel_id != Channel.Id && guild_id != Channel.GuildId)
                    {
                        var disconnectRequest = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.DisconnectRequest };
                        await SendRequestAsync(disconnectRequest);
                    }
                }

                var status = await ReserveCallResourcesAsync();
                if (status != VoipPhoneCallResourceReservationStatus.Success)
                    throw new Exception("Unable to reserve call resources!");

                App.Discord.VoiceStateUpdated += OnVoiceStateUpdated;
                App.Discord.VoiceServerUpdated += OnVoiceServerUpdated;

                SendVoiceStateUpdate(_state, Channel.Id);

                var vstu = await _voiceStateUpdateCompletion.Task.ConfigureAwait(false);
                var vsru = await _voiceServerUpdateCompletion.Task.ConfigureAwait(false);

                var connectionRequest = new ValueSet()
                {
                    ["req"] = (uint)VoiceServiceRequest.GuildConnectRequest,
                    ["channel_id"] = Channel.Id,
                    ["guild_id"] = Channel.Guild.Id,
                    ["user_id"] = App.Discord.CurrentUser.Id,
                    ["endpoint"] = vsru.Endpoint,
                    ["token"] = vsru.VoiceToken,
                    ["session_id"] = vstu.SessionId,
                    ["contact_name"] = $"{Channel.Name} - {Channel.Guild.Name}"
                };

                await SendRequestAsync(connectionRequest);
            }
        }

        private async Task<VoipPhoneCallResourceReservationStatus> ReserveCallResourcesAsync()
        {
            try
            {
                return await _voipCallCoordinator.ReserveCallResourcesAsync("Unicord.Universal.Voice.Background.VoiceBackgroundTask");
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147024713)
                    return VoipPhoneCallResourceReservationStatus.Success;
                else
                    throw;
            }
        }

        private async Task ToggleMuteAsync()
        {
            var set = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.MuteRequest, ["muted"] = Muted };
            await SendRequestAsync(set);
        }

        private async Task ToggleDeafenAsync()
        {
            var set = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.DeafenRequest, ["deafened"] = Deafened };
            await SendRequestAsync(set);
        }

        public async Task DisconnectAsync()
        {
            var set = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.DisconnectRequest };
            await SendRequestAsync(set);
        }

        private async Task<ValueSet> SendRequestAsync(ValueSet request)
        {
            var response = await _appServiceConnection.SendMessageAsync(request);
            if (response.Message != null)
            {
                // double cast because C#
                if ((VoiceServiceRequest)(uint)response.Message["req"] == VoiceServiceRequest.RequestFailed)
                {
                    throw new Exception((string)response.Message["msg"]);
                }

                return response.Message;
            }

            return null;
        }

        private void SendVoiceStateUpdate(VoiceState state, ulong? channel_id)
        {
            var vsd = new VoiceDispatch
            {
                OpCode = 4,
                Payload = new VoiceStateUpdatePayload
                {
                    GuildId = Channel.Guild.Id,
                    ChannelId = channel_id,
                    Deafened = channel_id != null ? (bool?)state.HasFlag(VoiceState.Deafened) : null,
                    Muted = channel_id != null ? (bool?)(state != VoiceState.None) : null
                }
            };

            var vsj = JsonConvert.SerializeObject(vsd, Formatting.None);
            App.Discord._webSocketClient.SendMessage(vsj);
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (args.Request.Message.TryGetValue("ev", out var ev))
            {
                var serviceEvent = (VoiceServiceEvent)(uint)ev;
                switch (serviceEvent)
                {
                    case VoiceServiceEvent.Connected:
                        break;
                    case VoiceServiceEvent.Reconnecting:
                        break;
                    case VoiceServiceEvent.Disconnected:
                        Disconnected?.Invoke(this, null);
                        SendVoiceStateUpdate(VoiceState.None, null);
                        break;
                    case VoiceServiceEvent.Muted:
                        SendVoiceStateUpdate(_state, Channel.Id);
                        break;
                    case VoiceServiceEvent.Deafened:
                        SendVoiceStateUpdate(_state, Channel.Id);
                        break;
                    case VoiceServiceEvent.UdpPing:
                        UdpPing = (uint)args.Request.Message["ping"];
                        break;
                    case VoiceServiceEvent.WebSocketPing:
                        WebSocketPing = (uint)args.Request.Message["ping"];
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {

        }

        private Task OnVoiceStateUpdated(VoiceStateUpdateEventArgs e)
        {
            if (e.Channel == Channel && e.User == App.Discord.CurrentUser)
            {
                _voiceStateUpdateCompletion.SetResult(e);
                App.Discord.VoiceStateUpdated -= OnVoiceStateUpdated;
            }

            return Task.CompletedTask;
        }

        private Task OnVoiceServerUpdated(VoiceServerUpdateEventArgs e)
        {
            if (e.Guild == Channel.Guild)
            {
                _voiceServerUpdateCompletion.SetResult(e);
                App.Discord.VoiceServerUpdated -= OnVoiceServerUpdated;
            }

            return Task.CompletedTask;
        }
    }

    [Flags]
    public enum VoiceState
    {
        None = 0,
        Deafened = 1,
        Muted = 2
    }
}
