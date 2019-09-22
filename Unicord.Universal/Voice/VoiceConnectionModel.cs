﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext.Entities;
using Newtonsoft.Json;
using Unicord.Universal.Converters;
using Unicord.Universal.Voice.Background;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Calls;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Voice
{
    public class VoiceConnectionModel : PropertyChangedBase, IDisposable
    {
        private MediaPlayerElement _mediaPlayer;
        private AppServiceConnection _appServiceConnection;
        private VoipCallCoordinator _voipCallCoordinator;
        private ResourceLoader _strings;

        private TaskCompletionSource<VoiceStateUpdateEventArgs> _voiceStateUpdateCompletion;
        private TaskCompletionSource<VoiceServerUpdateEventArgs> _voiceServerUpdateCompletion;

        private bool _muted;
        private bool _deafened;
        private bool _appServiceConnected;
        private uint _webSocketPing;
        private uint _udpPing;
        private string _connectionStatus;

        public string ConnectionStatus { get => _connectionStatus; set => OnPropertySet(ref _connectionStatus, value); }
        public DiscordChannel Channel { get; private set; }

        public bool Muted
        {
            get => _muted || _deafened;
            set => OnPropertySet(ref _muted, value);
        }

        public bool Deafened
        {
            get => _deafened;
            set
            {
                OnPropertySet(ref _deafened, value);
                InvokePropertyChanged(nameof(Muted));
            }
        }

        public uint WebSocketPing { get => _webSocketPing; set => OnPropertySet(ref _webSocketPing, value); }
        public uint UdpPing { get => _udpPing; set => OnPropertySet(ref _udpPing, value); }
        public bool IsDisposed { get; private set; }

        public event EventHandler<EventArgs> Disconnected;

        /// <summary>
        /// If voice is connected before Unicord launched, returns a VoiceConnectionModel for 
        /// that connection, otherwise null.
        /// </summary>
        public static async Task<VoiceConnectionModel> FindExistingConnectionAsync()
        {
            var connection = new AppServiceConnection()
            {
                AppServiceName = "com.wankerr.Unicord.Voice",
                PackageFamilyName = Package.Current.Id.FamilyName
            };

            if (await connection.OpenAsync() != AppServiceConnectionStatus.Success)
            {
                connection.Dispose();
                return null;
            }

            var strings = ResourceLoader.GetForViewIndependentUse("Voice");
            var stateRequest = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.StateRequest };
            var info = await connection.SendMessageAsync(stateRequest);
            var state = (VoiceServiceState)(uint)info.Message["state"];
            if (state == VoiceServiceState.ReadyToConnect)
            {
                connection.Dispose();
                return null;
            }

            var channel_id = (ulong)info.Message["channel_id"];
            var guild_id = (ulong)info.Message["guild_id"];
            var muted = (bool)info.Message["muted"];
            var deafened = (bool)info.Message["deafened"];

            var channel = App.Discord._channelCache[channel_id];
            var vstate = VoiceState.None;

            var model = new VoiceConnectionModel(channel, connection) { Muted = muted, Deafened = deafened, ConnectionStatus = string.Format(strings.GetString("ConnectedStateFormat"), channel.Name), _strings = strings };
            return model;
        }

        private VoiceConnectionModel()
        {
            _mediaPlayer = new MediaPlayerElement() { AutoPlay = true };
            _strings = ResourceLoader.GetForViewIndependentUse("Voice");
            _voiceStateUpdateCompletion = new TaskCompletionSource<VoiceStateUpdateEventArgs>();
            _voiceServerUpdateCompletion = new TaskCompletionSource<VoiceServerUpdateEventArgs>();
            _voipCallCoordinator = VoipCallCoordinator.GetDefault();

            ConnectionStatus = _strings.GetString("InitialConnectionState");
        }

        public VoiceConnectionModel(DiscordChannel channel) : this()
        {
            Channel = channel;
            _appServiceConnection = new AppServiceConnection()
            {
                AppServiceName = "com.wankerr.Unicord.Voice",
                PackageFamilyName = Package.Current.Id.FamilyName
            };

            _appServiceConnection.RequestReceived += OnRequestReceived;
            _appServiceConnection.ServiceClosed += OnServiceClosed;
        }

        private VoiceConnectionModel(DiscordChannel channel, AppServiceConnection connection) : this()
        {
            Channel = channel;
            _voipCallCoordinator = VoipCallCoordinator.GetDefault();
            _appServiceConnection = connection;
            _appServiceConnection.RequestReceived += OnRequestReceived;
            _appServiceConnection.ServiceClosed += OnServiceClosed;
        }

        public async Task UpdatePreferredAudioDevicesAsync(string audioRender, string audioCapture)
        {
            var set = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.SettingsUpdate };
            if (audioRender != null)
                set["output_device"] = audioRender;

            if (audioCapture != null)
                set["input_device"] = audioCapture;

            await SendRequestAsync(set);
        }

        public async Task MoveAsync(DiscordChannel newChannel)
        {
            if (Channel.Guild != newChannel.Guild)
            {
                throw new InvalidOperationException("Can only move inside a guild");
            }

            Channel = newChannel;
            ConnectionStatus = _strings.GetString("ConnectionState3");

            App.Discord.VoiceStateUpdated += OnVoiceStateUpdated;
            SendVoiceStateUpdate(Channel.Id);

            var vstu = await _voiceStateUpdateCompletion.Task.ConfigureAwait(false);
            ConnectionStatus = string.Format(_strings.GetString("ConnectionState4Format"), Channel.Name);

            var connectionRequest = new ValueSet()
            {
                ["req"] = (uint)VoiceServiceRequest.GuildMoveRequest,
                ["channel_id"] = Channel.Id,
                ["contact_name"] = Channel.Guild != null ? $"{Channel.Name} - {Channel.Guild.Name}" : DMNameConverter.Instance.Convert(Channel, null, null, null),
            };

            await SendRequestAsync(connectionRequest);
        }

        public async Task ConnectAsync()
        {
            if (ApiInformation.IsTypePresent("Windows.ApplicationModel.Calls.VoipPhoneCallResourceReservationStatus"))
            {
                ConnectionStatus = _strings.GetString("ConnectionState1");
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
                        ConnectionStatus = _strings.GetString("ConnectionState2");

                        var disconnectRequest = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.DisconnectRequest };
                        await SendRequestAsync(disconnectRequest);
                    }
                }

                _appServiceConnected = true;
                var status = await ReserveCallResourcesAsync();
                if (status != VoipPhoneCallResourceReservationStatus.Success)
                    throw new Exception("Unable to reserve call resources!");

                ConnectionStatus = _strings.GetString("ConnectionState3");

                App.Discord.VoiceStateUpdated += OnVoiceStateUpdated;
                App.Discord.VoiceServerUpdated += OnVoiceServerUpdated;
                SendVoiceStateUpdate(Channel.Id);

                var vstu = await _voiceStateUpdateCompletion.Task.ConfigureAwait(false);
                var vsru = await _voiceServerUpdateCompletion.Task.ConfigureAwait(false);
                var inputDeviceId = App.LocalSettings.Read<string>("InputDevice", null);
                var outputDeviceId = App.LocalSettings.Read<string>("OutputDevice", null);

                ConnectionStatus = string.Format(_strings.GetString("ConnectionState4Format"), Channel.Name);
                var connectionRequest = new ValueSet()
                {
                    ["req"] = Channel.Guild != null ? (uint)VoiceServiceRequest.GuildConnectRequest : (uint)VoiceServiceRequest.CallConnectRequest,
                    ["channel_id"] = Channel.Id,
                    ["guild_id"] = Channel.Guild?.Id ?? 0,
                    ["user_id"] = App.Discord.CurrentUser.Id,
                    ["endpoint"] = vsru.Endpoint,
                    ["token"] = vsru.VoiceToken,
                    ["session_id"] = vstu.SessionId,
                    ["contact_name"] = Channel.Guild != null ? $"{Channel.Name} - {Channel.Guild.Name}" : DMNameConverter.Instance.Convert(Channel, null, null, null),
                    ["input_device"] = inputDeviceId,
                    ["output_device"] = outputDeviceId,
                    ["muted"] = Muted,
                    ["deafened"] = Deafened
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

        public async Task ToggleMuteAsync()
        {
            Muted = !Muted;

            if (_appServiceConnected)
            {
                var set = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.MuteRequest, ["muted"] = Muted };
                await SendRequestAsync(set);
            }
        }

        public async Task ToggleDeafenAsync()
        {
            Deafened = !Deafened;

            if (_appServiceConnected)
            {
                var set = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.DeafenRequest, ["deafened"] = Deafened };
                await SendRequestAsync(set);
            }
        }

        public async Task DisconnectAsync()
        {
            var set = new ValueSet() { ["req"] = (uint)VoiceServiceRequest.DisconnectRequest };
            await SendRequestAsync(set);

            await PlayCueAsync("self_disconnected");
            ConnectionStatus = _strings.GetString("DisconnectedState");
            Disconnected?.Invoke(this, null);
            SendVoiceStateUpdate(null);
        }

        private async Task<ValueSet> SendRequestAsync(ValueSet request)
        {
            if (IsDisposed)
                return null;

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

        private void SendVoiceStateUpdate(ulong? channel_id)
        {
            var vsd = new VoiceDispatch
            {
                OpCode = 4,
                Payload = new VoiceStateUpdatePayload
                {
                    GuildId = Channel.Guild?.Id,
                    ChannelId = channel_id,
                    Deafened = _muted,
                    Muted = _deafened
                }
            };

            var vsj = JsonConvert.SerializeObject(vsd, Formatting.None);
            App.Discord._webSocketClient.SendMessage(vsj);
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (args.Request.Message.TryGetValue("ev", out var ev))
            {
                var serviceEvent = (VoiceServiceEvent)(uint)ev;
                switch (serviceEvent)
                {
                    case VoiceServiceEvent.Connected:
                        await PlayCueAsync("connected");
                        ConnectionStatus = string.Format(_strings.GetString("ConnectedStateFormat"), Channel.Name);
                        break;
                    case VoiceServiceEvent.Reconnecting:
                        await PlayCueAsync("self_disconnected");
                        ConnectionStatus = _strings.GetString("ReconnectingState");
                        break;
                    case VoiceServiceEvent.Disconnected:
                        await PlayCueAsync("self_disconnected");
                        ConnectionStatus = _strings.GetString("DisconnectedState");
                        Disconnected?.Invoke(this, null);
                        SendVoiceStateUpdate(null);
                        break;
                    case VoiceServiceEvent.Muted:
                        await PlayCueAsync(_muted ? "mute" : "unmute");
                        SendVoiceStateUpdate(Channel.Id);
                        break;
                    case VoiceServiceEvent.Deafened:
                        await PlayCueAsync(_deafened ? "deafen" : "undeafen");
                        SendVoiceStateUpdate(Channel.Id);
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
            _appServiceConnected = false;
            _appServiceConnection = null;
        }

        private Task OnVoiceStateUpdated(VoiceStateUpdateEventArgs e)
        {
            if (e.Channel == Channel && e.User == App.Discord.CurrentUser && !_voiceStateUpdateCompletion.Task.IsCompleted)
            {
                _voiceStateUpdateCompletion.SetResult(e);
            }

            if (e.User != App.Discord.CurrentUser)
            {
                if (e.After?.Channel == Channel && e.Before?.Channel == null)
                {
                    return PlayCueAsync("connected");
                }

                if (e.Before?.Channel == Channel && e.After?.Channel == null)
                {
                    return PlayCueAsync("user_disconnected");
                }
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

        private async Task PlayCueAsync(string cue)
        {
            try
            {
                var folder = await (await Package.Current.InstalledLocation.GetFolderAsync("Assets")).GetFolderAsync("Sounds");
                var file = await folder.GetFileAsync($"{cue}.mp3");

                await _mediaPlayer.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    _mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
                    _mediaPlayer.MediaPlayer.Play();
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to play sound cue \'{cue}\'");
                Logger.Log(ex);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;

            _appServiceConnected = false;

            _appServiceConnection?.Dispose();
            _appServiceConnection = null;
            _mediaPlayer.MediaPlayer?.Dispose();
            _mediaPlayer = null;

            App.Discord.VoiceStateUpdated -= OnVoiceStateUpdated;
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
