using Concentus.Structs;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Udp;
using DSharpPlus.Net.WebSocket;
using DSharpPlus.VoiceNext.Codec;
using DSharpPlus.VoiceNext.EventArgs;
using DSharpPlus.VoiceNext.VoiceEntities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace DSharpPlus.VoiceNext
{
    internal delegate void VoiceDisconnectedEventHandler(DiscordGuild guild);

    /// <summary>
    /// VoiceNext connection to a voice channel.
    /// </summary>
    public sealed class VoiceNextConnection : IDisposable
    {
        /// <summary>
        /// Triggered whenever a user speaks in the connected voice channel.
        /// </summary>
        public event AsyncEventHandler<UserSpeakingEventArgs> UserSpeaking
        {
            add { _userSpeaking.Register(value); }
            remove { _userSpeaking.Unregister(value); }
        }
        private AsyncEvent<UserSpeakingEventArgs> _userSpeaking;

        /// <summary>
        /// Triggered whenever a user leaves voice in the connected guild.
        /// </summary>
        public event AsyncEventHandler<VoiceUserLeaveEventArgs> UserLeft
        {
            add { _userLeft.Register(value); }
            remove { _userLeft.Unregister(value); }
        }
        private AsyncEvent<VoiceUserLeaveEventArgs> _userLeft;

#if !NETSTANDARD1_1
        /// <summary>
        /// Triggered whenever voice data is received from the connected voice channel.
        /// </summary>
        public event AsyncEventHandler<VoiceReceiveEventArgs> VoiceReceived
        {
            add { _voiceReceived.Register(value); }
            remove { _voiceReceived.Unregister(value); }
        }
        private AsyncEvent<VoiceReceiveEventArgs> _voiceReceived;
#endif

        /// <summary>
        /// Triggered whenever voice WebSocket throws an exception.
        /// </summary>
        public event AsyncEventHandler<SocketErrorEventArgs> VoiceSocketErrored
        {
            add { _voiceSocketError.Register(value); }
            remove { _voiceSocketError.Unregister(value); }
        }
        private AsyncEvent<SocketErrorEventArgs> _voiceSocketError;

        internal event VoiceDisconnectedEventHandler VoiceDisconnected;

        private const string VOICE_MODE = "xsalsa20_poly1305";

        private static DateTime UnixEpoch { get { return _unixEpoch.Value; } }
        private static Lazy<DateTime> _unixEpoch;

        private DiscordClient Discord { get; }
        private DiscordGuild Guild { get; }
        private ConcurrentDictionary<uint, ulong> SSRCMap { get; }

        private DatagramSocket UdpSocket { get; set; }
        private BaseWebSocketClient VoiceWs { get; set; }
        private Task HeartbeatTask { get; set; }
        private int HeartbeatInterval { get; set; }
        private DateTime LastHeartbeat { get; set; }

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token
            => TokenSource.Token;

        internal VoiceServerUpdatePayload ServerData { get; set; }
        internal VoiceStateUpdatePayload StateData { get; set; }
        internal bool Resume { get; set; }

        private VoiceNextConfiguration Configuration { get; }
        private SodiumCodec Sodium { get; set; }

        private OpusEncoder OpusEncoder { get; set; }
        private ConcurrentDictionary<uint, OpusDecoder> OpusDecoders { get; set; }
        private RtpCodec Rtp { get; set; }
        private double SynchronizerTicks { get; set; }
        private double SynchronizerResolution { get; set; }
        private double TickResolution { get; set; }

        private ushort Sequence { get; set; }
        private uint Timestamp { get; set; }
        private uint SSRC { get; set; }
        private byte[] Key { get; set; }
        private IPEndPoint DiscoveredEndpoint { get; set; }
        internal ConnectionEndpoint ConnectionEndpoint { get; set; }

        private TaskCompletionSource<bool> ReadyWait { get; set; }
        private bool IsInitialized { get; set; }
        private bool IsDisposed { get; set; }

        private TaskCompletionSource<bool> PlayingWait { get; set; }
        private SemaphoreSlim PlaybackSemaphore { get; set; }

#if !NETSTANDARD1_1
        private Task ReceiverTask { get; set; }
        private CancellationTokenSource ReceiverTokenSource { get; set; }
        private CancellationToken ReceiverToken
            => ReceiverTokenSource.Token;
#endif

        /// <summary>
        /// Gets whether this connection is still playing audio.
        /// </summary>
        public bool IsPlaying
            => PlaybackSemaphore.CurrentCount == 0 || (PlayingWait != null && !PlayingWait.Task.IsCompleted);

        /// <summary>
        /// Gets the websocket round-trip time in ms.
        /// </summary>
        public int Ping
            => Volatile.Read(ref _ping);

        private int _ping = 0;

        /// <summary>
        /// Gets the channel this voice client is connected to.
        /// </summary>
        public DiscordChannel Channel { get; internal set; }
        public IOutputStream OutputStream { get; private set; }

        internal VoiceNextConnection(DiscordClient client, DiscordGuild guild, DiscordChannel channel, VoiceNextConfiguration config, VoiceServerUpdatePayload server, VoiceStateUpdatePayload state)
        {
            Discord = client;
            Guild = guild;
            Channel = channel;
            SSRCMap = new ConcurrentDictionary<uint, ulong>();

            _userSpeaking = new AsyncEvent<UserSpeakingEventArgs>(Discord.EventErrorHandler, "USER_SPEAKING");
            _userLeft = new AsyncEvent<VoiceUserLeaveEventArgs>(Discord.EventErrorHandler, "USER_LEFT");
            _voiceReceived = new AsyncEvent<VoiceReceiveEventArgs>(Discord.EventErrorHandler, "VOICE_RECEIVED");
            _voiceSocketError = new AsyncEvent<SocketErrorEventArgs>(Discord.EventErrorHandler, "VOICE_WS_ERROR");
            TokenSource = new CancellationTokenSource();

            Configuration = config;
            OpusDecoders = new ConcurrentDictionary<uint, OpusDecoder>();
            OpusEncoder = new OpusEncoder(48000, 2, Configuration.VoiceApplication) { Bitrate = 13107, UseVBR = true };
            Sodium = new SodiumCodec();
            Rtp = new RtpCodec();

            ServerData = server;
            StateData = state;

            var eps = ServerData.Endpoint;
            var epi = eps.LastIndexOf(':');
            var eph = string.Empty;
            var epp = 80;
            if (epi != -1)
            {
                eph = eps.Substring(0, epi);
                epp = int.Parse(eps.Substring(epi + 1));
            }
            else
            {
                eph = eps;
            }
            ConnectionEndpoint = new ConnectionEndpoint { Hostname = eph, Port = epp };

            ReadyWait = new TaskCompletionSource<bool>();
            IsInitialized = false;
            IsDisposed = false;

            PlayingWait = null;
            PlaybackSemaphore = new SemaphoreSlim(1, 1);

            UdpSocket = new DatagramSocket();

            VoiceWs = Discord.Configuration.WebSocketClientFactory(Discord.Configuration.Proxy);
            VoiceWs.OnDisconnect += VoiceWS_SocketClosed;
            VoiceWs.OnMessage += VoiceWS_SocketMessage;
            VoiceWs.OnConnect += VoiceWS_SocketOpened;
            VoiceWs.OnError += VoiceWs_SocketErrored;
        }


        static VoiceNextConnection()
        {
            _unixEpoch = new Lazy<DateTime>(() => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        ~VoiceNextConnection()
        {
            Dispose();
        }

        /// <summary>
        /// Connects to the specified voice channel.
        /// </summary>
        /// <returns>A task representing the connection operation.</returns>
        internal Task ConnectAsync()
        {
            var gwuri = new UriBuilder
            {
                Scheme = "wss",
                Host = ConnectionEndpoint.Hostname,
                Query = "encoding=json&v=4"
            };

            return VoiceWs.ConnectAsync(gwuri.Uri);
        }

        internal Task ReconnectAsync()
            => VoiceWs.DisconnectAsync(new SocketCloseEventArgs(Discord));

        internal Task StartAsync()
        {
            // Let's announce our intentions to the server
            var vdp = new VoiceDispatch();

            if (!Resume)
            {
                vdp.OpCode = 0;
                vdp.Payload = new VoiceIdentifyPayload
                {
                    ServerId = ServerData.GuildId,
                    UserId = StateData.UserId.Value,
                    SessionId = StateData.SessionId,
                    Token = ServerData.Token
                };
                Resume = true;
            }
            else
            {
                vdp.OpCode = 7;
                vdp.Payload = new VoiceIdentifyPayload
                {
                    ServerId = ServerData.GuildId,
                    SessionId = StateData.SessionId,
                    Token = ServerData.Token
                };
            }
            var vdj = JsonConvert.SerializeObject(vdp, Formatting.None);
            VoiceWs.SendMessage(vdj);

            return Task.Delay(0);
        }

        internal Task WaitForReadyAsync()
            => ReadyWait.Task;

        /// <summary>
        /// Encodes, encrypts, and sends the provided PCM data to the connected voice channel.
        /// </summary>
        /// <param name="pcmData">PCM data to encode, encrypt, and send.</param>
        /// <param name="blockSize">Millisecond length of the PCM data.</param>
        /// <param name="bitRate">Bitrate of the PCM data.</param>
        /// <returns>Task representing the sending operation.</returns>
        public async Task SendAsync(byte[] pcmData, int blockSize, int bitRate = 16)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("The connection is not initialized");

            await PlaybackSemaphore.WaitAsync().ConfigureAwait(false);

            var rtp = Rtp.Encode(Sequence, Timestamp, SSRC);

            var dat = OpusCodec.Encode(pcmData, 0, pcmData.Length, OpusEncoder, bitRate);
            dat = Sodium.Encode(dat, Rtp.MakeNonce(rtp), Key);
            dat = Rtp.Encode(rtp, dat);

            if (SynchronizerTicks == 0)
            {
                SynchronizerTicks = Stopwatch.GetTimestamp();
                SynchronizerResolution = (Stopwatch.Frequency * 0.02);
                TickResolution = 10_000_000.0 / Stopwatch.Frequency;
                Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Timer accuracy: {Stopwatch.Frequency.ToString("#,##0", CultureInfo.InvariantCulture)}/{SynchronizerResolution.ToString(CultureInfo.InvariantCulture)} (high resolution? {Stopwatch.IsHighResolution})", DateTime.Now);
            }
            else
            {
                // Provided by Laura#0090 (214796473689178133); this is Python, but adaptable:
                // 
                // delay = max(0, self.delay + ((start_time + self.delay * loops) + - time.time()))
                // 
                // self.delay
                //   sample size
                // start_time
                //   time since streaming started
                // loops
                //   number of samples sent
                // time.time()
                //   DateTime.Now

                var cts = Math.Max(Stopwatch.GetTimestamp() - SynchronizerTicks, 0);
                if (cts < SynchronizerResolution)
                    await Task.Delay(TimeSpan.FromTicks((long)((SynchronizerResolution - cts) * TickResolution))).ConfigureAwait(false);

                SynchronizerTicks += SynchronizerResolution;
            }

            await SendSpeakingAsync(true).ConfigureAwait(false);
            await OutputStream.WriteAsync(dat.AsBuffer());

            Sequence++;
            Timestamp += 48 * (uint)blockSize;

            PlaybackSemaphore.Release();
        }

        private async void UdpSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            byte[] data = null;
            byte[] header = null;

            short[] voice = null;

            ushort seq = 0;
            uint ts = 0, ssrc = 0;
            var length = 0;
            try
            {
                using (var reader = args.GetDataReader())
                {
                    var inStream = args.GetDataStream();
                    var buffer = WindowsRuntimeBuffer.Create(3840);
                    buffer = await inStream.ReadAsync(buffer, 3840, InputStreamOptions.ReadAhead);
                    data = buffer.ToArray();

                    header = new byte[RtpCodec.SIZE_HEADER];
                    data = Rtp.Decode(data, header);

                    var nonce = Rtp.MakeNonce(header);
                    data = Sodium.Decode(data, nonce, Key);

                    // following is thanks to code from Eris
                    // https://github.com/abalabahaha/eris/blob/master/lib/voice/VoiceConnection.js#L623
                    var doff = 0;
                    Rtp.Decode(header, out seq, out ts, out ssrc, out var has_ext);
                    if (has_ext)
                    {
                        if (data[0] == 0xBE && data[1] == 0xDE)
                        {
                            // RFC 5285, 4.2 One-Byte header
                            // http://www.rfcreader.com/#rfc5285_line186

                            var hlen = data[2] << 8 | data[3];
                            var i = 4;
                            for (; i < hlen + 4; i++)
                            {
                                var b = data[i];
                                // This is unused(?)
                                //var id = (b >> 4) & 0x0F;
                                var len = (b & 0x0F) + 1;
                                i += len;
                            }
                            while (data[i] == 0)
                                i++;
                            doff = i;
                        }
                        // TODO: consider implementing RFC 5285, 4.3. Two-Byte Header
                    }

                    OpusDecoder decoder;
                    if (!OpusDecoders.TryGetValue(ssrc, out decoder))
                    {
                        decoder = new OpusDecoder(48000, 2);
                    }

                    voice = OpusCodec.Decode(data, doff, data.Length - doff, decoder, ref length);
                }
            }
            catch (Exception ex)
            {
                Discord.DebugLogger.LogMessage(LogLevel.Critical, "VoiceNext", ex.ToString(), DateTime.Now);
                return;
            }

            // TODO: wait for ssrc map?
            DiscordUser user = null;
            if (SSRCMap.ContainsKey(ssrc))
            {
                var id = SSRCMap[ssrc];
                if (Guild != null)
                    user = Guild._members.FirstOrDefault(xm => xm.Id == id) ?? await Guild.GetMemberAsync(id).ConfigureAwait(false);

                if (user == null)
                    user = Discord.InternalGetCachedUser(id);

                if (user == null)
                    user = new DiscordUser { Discord = Discord, Id = id };
            }

            await _voiceReceived.InvokeAsync(new VoiceReceiveEventArgs(Discord)
            {
                SSRC = ssrc,
                Voice = new ReadOnlyCollection<short>(voice),
                VoiceLength = length,
                User = user
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a speaking status to the connected voice channel.
        /// </summary>
        /// <param name="speaking">Whether the current user is speaking or not.</param>
        /// <returns>A task representing the sending operation.</returns>
        public async Task SendSpeakingAsync(bool speaking = true)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("The connection is not initialized");

            if (!speaking)
            {
                var nullpcm = new byte[3840];
                for (var i = 0; i < 5; i++)
                    await SendAsync(nullpcm, 20).ConfigureAwait(false);

                SynchronizerTicks = 0;
                if (PlayingWait != null)
                    PlayingWait.SetResult(true);
            }
            else
            {
                if (PlayingWait == null || PlayingWait.Task.IsCompleted)
                    PlayingWait = new TaskCompletionSource<bool>();
            }

            var pld = new VoiceDispatch
            {
                OpCode = 5,
                Payload = new VoiceSpeakingPayload
                {
                    Speaking = speaking,
                    Delay = 0
                }
            };

            var plj = JsonConvert.SerializeObject(pld, Formatting.None);
            VoiceWs.SendMessage(plj);
        }

        /// <summary>
        /// Asynchronously waits for playback to be finished. Playback is finished when speaking = false is signalled.
        /// </summary>
        /// <returns>A task representing the waiting operation.</returns>
        public async Task WaitForPlaybackFinishAsync()
        {
            if (PlayingWait != null)
                await PlayingWait.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Disconnects and disposes this voice connection.
        /// </summary>
        public void Disconnect()
            => Dispose();

        /// <summary>
        /// Disconnects and disposes this voice connection.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            IsInitialized = false;
            TokenSource.Cancel();
#if !NETSTANDARD1_1
            if (Configuration.EnableIncoming)
                ReceiverTokenSource.Cancel();
#endif

            try
            {
                VoiceWs.DisconnectAsync(null).ConfigureAwait(false).GetAwaiter().GetResult();
                UdpSocket.Dispose();
            }
            catch (Exception)
            { }

            Sodium = null;
            Rtp = null;

            VoiceDisconnected?.Invoke(Guild);
        }

        private async Task Heartbeat()
        {
            await Task.Yield();

            var token = Token;
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    var dt = DateTime.Now;
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "Sent heartbeat", dt);

                    var hbd = new VoiceDispatch
                    {
                        OpCode = 3,
                        Payload = UnixTimestamp(dt)
                    };
                    var hbj = JsonConvert.SerializeObject(hbd);
                    VoiceWs.SendMessage(hbj);

                    LastHeartbeat = dt;
                    await Task.Delay(HeartbeatInterval).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task Stage1()
        {
            // IP Discovery
            async void MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
            {
                var inStream = args.GetDataStream();
                var buffer = WindowsRuntimeBuffer.Create(70);
                buffer = await inStream.ReadAsync(buffer, 70, InputStreamOptions.Partial);
                var ipd = buffer.ToArray();

                inStream.Dispose();

                UdpSocket.MessageReceived -= MessageReceived;
                UdpSocket.MessageReceived += UdpSocket_MessageReceived;

                var ipe = Array.IndexOf<byte>(ipd, 0, 4);
                var ip = new UTF8Encoding(false).GetString(ipd, 4, ipe - 4);
                var port = BitConverter.ToUInt16(ipd, ipd.Length - 2);
                DiscoveredEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);

                var remoteHostName = new HostName(DiscoveredEndpoint.Address.ToString());

                // Ready
                var vsp = new VoiceDispatch
                {
                    OpCode = 1,
                    Payload = new VoiceSelectProtocolPayload
                    {
                        Protocol = "udp",
                        Data = new VoiceSelectProtocolPayloadData
                        {
                            Address = DiscoveredEndpoint.Address.ToString(),
                            Port = (ushort)DiscoveredEndpoint.Port,
                            Mode = VOICE_MODE
                        },
                        Address = DiscoveredEndpoint.Address.ToString(),
                        Port = (ushort)DiscoveredEndpoint.Port,
                        Mode = VOICE_MODE
                    }
                };
                var vsj = JsonConvert.SerializeObject(vsp, Formatting.None);
                VoiceWs.SendMessage(vsj);
            }

            await UdpSocket.BindServiceNameAsync(ConnectionEndpoint.Port.ToString());
            OutputStream = await UdpSocket.GetOutputStreamAsync(new HostName(ConnectionEndpoint.Hostname), ConnectionEndpoint.Port.ToString());

            var pck = new byte[70];
            Array.Copy(BitConverter.GetBytes(SSRC), 0, pck, pck.Length - 4, 4);

            await OutputStream.WriteAsync(pck.AsBuffer());
            await OutputStream.FlushAsync();

            UdpSocket.MessageReceived += MessageReceived;
        }

        private Task Stage2()
        {
            IsInitialized = true;
            ReadyWait.SetResult(true);
            return Task.Delay(0);
        }

        private async Task HandleDispatch(JObject jo)
        {
            var opc = (int)jo["op"];
            var opp = jo["d"] as JObject;

            //Debug.WriteLine(UdpSocket.Control.QualityOfService);

            switch (opc)
            {
                case 2:
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP2 received", DateTime.Now);
                    var vrp = opp.ToObject<VoiceReadyPayload>();
                    SSRC = vrp.SSRC;
                    ConnectionEndpoint = new ConnectionEndpoint { Hostname = ConnectionEndpoint.Hostname, Port = vrp.Port };
                    await Stage1().ConfigureAwait(false);
                    break;

                case 4:
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP4 received", DateTime.Now);
                    var vsd = opp.ToObject<VoiceSessionDescriptionPayload>();
                    Key = vsd.SecretKey;

                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Using {vsd.Mode}", DateTime.Now);

                    await Stage2().ConfigureAwait(false);
                    break;

                case 5:
                    // Don't spam OP5
                    this.Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP5 received", DateTime.Now);
                    var spd = opp.ToObject<VoiceSpeakingPayload>();
                    var spk = new UserSpeakingEventArgs(Discord)
                    {
                        Speaking = spd.Speaking,
                        SSRC = spd.SSRC.Value,
                        User = Discord.InternalGetCachedUser(spd.UserId.Value)
                    };
                    if (!SSRCMap.ContainsKey(spk.SSRC))
                        SSRCMap.AddOrUpdate(spk.SSRC, spk.User.Id, (k, v) => spk.User.Id);
                    await _userSpeaking.InvokeAsync(spk).ConfigureAwait(false);
                    break;

                case 6:
                    var dt = DateTime.Now;
                    var ping = (int)(dt - LastHeartbeat).TotalMilliseconds;
                    Volatile.Write(ref _ping, ping);
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Received voice heartbeat ACK, ping {ping.ToString("#,##0", CultureInfo.InvariantCulture)}ms", dt);
                    LastHeartbeat = dt;
                    break;

                case 8:
                    // this sends a heartbeat interval that appears to be consistent with regular GW hello
                    // however opcodes don't match (8 != 10)
                    // so we suppress it so that users are not alerted
                    // HELLO

                    var interval = opp["heartbeat_interval"].ToObject<int>();

                    HeartbeatInterval = interval;
                    HeartbeatTask = Task.Run(Heartbeat);

                    break;

                case 9:
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP9 received", DateTime.Now);
                    HeartbeatTask = Task.Run(Heartbeat);
                    break;

                case 13:
                    var ulpd = opp.ToObject<VoiceUserLeavePayload>();
                    var usr = await Discord.GetUserAsync(ulpd.UserId).ConfigureAwait(false);
                    var ssrc = SSRCMap.FirstOrDefault(x => x.Value == ulpd.UserId);
                    if (ssrc.Value != 0)
                        SSRCMap.TryRemove(ssrc.Key, out _);
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"User '{usr.Username}#{usr.Discriminator}' ({ulpd.UserId.ToString(CultureInfo.InvariantCulture)}) left voice chat in '{Channel.Guild.Name}' ({Channel.Guild.Id.ToString(CultureInfo.InvariantCulture)})", DateTime.Now);
                    await _userLeft.InvokeAsync(new VoiceUserLeaveEventArgs(Discord) { User = usr }).ConfigureAwait(false);
                    break;

                default:
                    Discord.DebugLogger.LogMessage(LogLevel.Warning, "VoiceNext", $"Unknown opcode received: {opc.ToString(CultureInfo.InvariantCulture)}", DateTime.Now);
                    break;
            }
        }

        private async Task VoiceWS_SocketClosed(SocketCloseEventArgs e)
        {
            Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Voice socket closed ({e.CloseCode.ToString(CultureInfo.InvariantCulture)}, '{e.CloseMessage}')", DateTime.Now);

            // generally this should not be disposed on all disconnects, only on requested ones
            // or something
            // otherwise problems happen
            //this.Dispose();

            if (e.CloseCode == 4006 || e.CloseCode == 4009)
                Resume = false;

            if (!IsDisposed)
            {
                TokenSource.Cancel();
                TokenSource = new CancellationTokenSource();
                VoiceWs = Discord.Configuration.WebSocketClientFactory(Discord.Configuration.Proxy);
                VoiceWs.OnDisconnect += VoiceWS_SocketClosed;
                VoiceWs.OnMessage += VoiceWS_SocketMessage;
                VoiceWs.OnConnect += VoiceWS_SocketOpened;
                await ConnectAsync().ConfigureAwait(false);
            }
        }

        private Task VoiceWS_SocketMessage(SocketMessageEventArgs e)
            => HandleDispatch(JObject.Parse(e.Message));

        private Task VoiceWS_SocketOpened()
            => StartAsync();

        private Task VoiceWs_SocketErrored(SocketErrorEventArgs e)
            => _voiceSocketError.InvokeAsync(new SocketErrorEventArgs(Discord) { Exception = e.Exception });

        private static uint UnixTimestamp(DateTime dt)
        {
            var ts = dt - UnixEpoch;
            var sd = ts.TotalSeconds;
            var si = (uint)sd;
            return si;
        }
    }
}

// Naam you still owe me those noodles :^)
// I remember