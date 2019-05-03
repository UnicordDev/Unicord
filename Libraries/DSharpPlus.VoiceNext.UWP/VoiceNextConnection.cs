using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.Net.Udp;
using DSharpPlus.Net.WebSocket;
using DSharpPlus.VoiceNext.Codec;
using DSharpPlus.VoiceNext.Entities;
using DSharpPlus.VoiceNext.EventArgs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        /// Triggered whenever a user joins voice in the connected guild.
        /// </summary>
        public event AsyncEventHandler<VoiceUserJoinEventArgs> UserJoined
        {
            add { _userJoined.Register(value); }
            remove { _userJoined.Unregister(value); }
        }
        private AsyncEvent<VoiceUserJoinEventArgs> _userJoined;

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

        private static DateTimeOffset UnixEpoch { get; } = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private DiscordClient Discord { get; }
        private DiscordGuild Guild { get; }
#if !NETSTANDARD1_1
        private ConcurrentDictionary<uint, AudioSender> TransmittingSSRCs { get; }
#endif

        private BaseUdpClient UdpClient { get; }
        private BaseWebSocketClient VoiceWs { get; set; }
        private Task HeartbeatTask { get; set; }
        private int HeartbeatInterval { get; set; }
        private DateTimeOffset LastHeartbeat { get; set; }

        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token
            => TokenSource.Token;

        internal VoiceServerUpdatePayload ServerData { get; set; }
        internal VoiceStateUpdatePayload StateData { get; set; }
        internal bool Resume { get; set; }

        private VoiceNextConfiguration Configuration { get; }
        private Opus Opus { get; set; }
        private Sodium Sodium { get; set; }
        private Rtp Rtp { get; set; }
        private EncryptionMode SelectedEncryptionMode { get; set; }
        private uint Nonce { get; set; } = 0;

        private ushort Sequence { get; set; }
        private uint Timestamp { get; set; }
        private uint SSRC { get; set; }
        private byte[] Key { get; set; }
#if !NETSTANDARD1_1
        private IPEndPoint DiscoveredEndpoint { get; set; }
#endif
        internal ConnectionEndpoint ConnectionEndpoint { get; set; }

        private TaskCompletionSource<bool> ReadyWait { get; set; }
        private bool IsInitialized { get; set; }
        private bool IsDisposed { get; set; }

        private TaskCompletionSource<bool> PlayingWait { get; set; }

        private ConcurrentQueue<VoicePacket> PacketQueue { get; }
        private VoiceTransmitStream TransmitStream { get; set; }
        private ConcurrentDictionary<ulong, long> KeepaliveTimestamps { get; }
        private ulong _lastKeepalive = 0;

        private Task SenderTask { get; set; }
        private CancellationTokenSource SenderTokenSource { get; set; }
        private CancellationToken SenderToken
            => SenderTokenSource.Token;

        private Task ReceiverTask { get; set; }
        private CancellationTokenSource ReceiverTokenSource { get; set; }
        private CancellationToken ReceiverToken
            => ReceiverTokenSource.Token;

        private Task KeepaliveTask { get; set; }
        private CancellationTokenSource KeepaliveTokenSource { get; set; }
        private CancellationToken KeepaliveToken
            => KeepaliveTokenSource.Token;

        /// <summary>
        /// Gets the audio format used by the Opus encoder.
        /// </summary>
        public AudioFormat AudioFormat => Configuration.AudioFormat;

        /// <summary>
        /// Gets whether this connection is still playing audio.
        /// </summary>
        public bool IsPlaying
            => PlayingWait != null && !PlayingWait.Task.IsCompleted;

        /// <summary>
        /// Gets the websocket round-trip time in ms.
        /// </summary>
        public int WebSocketPing
            => Volatile.Read(ref _wsPing);
        private int _wsPing = 0;

        /// <summary>
        /// Gets the UDP round-trip time in ms.
        /// </summary>
        public int UdpPing
            => Volatile.Read(ref _udpPing);
        private int _udpPing = 0;

        /// <summary>
        /// Gets the channel this voice client is connected to.
        /// </summary>
        public DiscordChannel Channel { get; internal set; }

        internal VoiceNextConnection(DiscordClient client, DiscordGuild guild, DiscordChannel channel, VoiceNextConfiguration config, VoiceServerUpdatePayload server, VoiceStateUpdatePayload state)
        {
            Discord = client;
            Guild = guild;
            Channel = channel;
#if !NETSTANDARD1_1
            TransmittingSSRCs = new ConcurrentDictionary<uint, AudioSender>();
#endif

            _userSpeaking = new AsyncEvent<UserSpeakingEventArgs>(Discord.EventErrorHandler, "VNEXT_USER_SPEAKING");
            _userJoined = new AsyncEvent<VoiceUserJoinEventArgs>(Discord.EventErrorHandler, "VNEXT_USER_JOINED");
            _userLeft = new AsyncEvent<VoiceUserLeaveEventArgs>(Discord.EventErrorHandler, "VNEXT_USER_LEFT");
#if !NETSTANDARD1_1
            _voiceReceived = new AsyncEvent<VoiceReceiveEventArgs>(Discord.EventErrorHandler, "VNEXT_VOICE_RECEIVED");
#endif
            _voiceSocketError = new AsyncEvent<SocketErrorEventArgs>(Discord.EventErrorHandler, "VNEXT_WS_ERROR");
            TokenSource = new CancellationTokenSource();

            Configuration = config;
            Opus = new Opus(AudioFormat);
            //this.Sodium = new Sodium();
            Rtp = new Rtp();

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
            PacketQueue = new ConcurrentQueue<VoicePacket>();
            KeepaliveTimestamps = new ConcurrentDictionary<ulong, long>();

            UdpClient = Discord.Configuration.UdpClientFactory();
            VoiceWs = Discord.Configuration.WebSocketClientFactory(Discord.Configuration.Proxy);
            VoiceWs.Disconnected += VoiceWS_SocketClosed;
            VoiceWs.MessageRecieved += VoiceWS_SocketMessage;
            VoiceWs.Connected += VoiceWS_SocketOpened;
            VoiceWs.Errored += VoiceWs_SocketErrored;
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

        internal void PreparePacket(ReadOnlySpan<byte> pcm, ref Memory<byte> target)
        {
            var audioFormat = AudioFormat;

            var packetArray = ArrayPool<byte>.Shared.Rent(Rtp.CalculatePacketSize(audioFormat.SampleCountToSampleSize(audioFormat.CalculateMaximumFrameSize()), SelectedEncryptionMode));
            var packet = packetArray.AsSpan();

            Rtp.EncodeHeader(Sequence, Timestamp, SSRC, packet);
            var opus = packet.Slice(Rtp.HeaderSize, pcm.Length);
            Opus.Encode(pcm, ref opus);

            Sequence++;
            Timestamp += (uint)audioFormat.CalculateFrameSize(audioFormat.CalculateSampleDuration(pcm.Length));

            Span<byte> nonce = stackalloc byte[Sodium.NonceSize];
            switch (SelectedEncryptionMode)
            {
                case EncryptionMode.XSalsa20_Poly1305:
                    Sodium.GenerateNonce(packet.Slice(0, Rtp.HeaderSize), nonce);
                    break;

#if !NETSTANDARD1_1
                case EncryptionMode.XSalsa20_Poly1305_Suffix:
                    Sodium.GenerateNonce(nonce);
                    break;
#endif

                case EncryptionMode.XSalsa20_Poly1305_Lite:
                    Sodium.GenerateNonce(Nonce++, nonce);
                    break;

                default:
                    ArrayPool<byte>.Shared.Return(packetArray);
                    throw new Exception("Unsupported encryption mode.");
            }

            Span<byte> encrypted = stackalloc byte[Sodium.CalculateTargetSize(opus)];
            Sodium.Encrypt(opus, encrypted, nonce);
            encrypted.CopyTo(packet.Slice(Rtp.HeaderSize));
            packet = packet.Slice(0, Rtp.CalculatePacketSize(encrypted.Length, SelectedEncryptionMode));
            Sodium.AppendNonce(nonce, packet, SelectedEncryptionMode);

            target = target.Slice(0, packet.Length);
            packet.CopyTo(target.Span);
            ArrayPool<byte>.Shared.Return(packetArray);
        }

        internal void EnqueuePacket(VoicePacket packet)
            => PacketQueue.Enqueue(packet);

        private async Task VoiceSenderTask()
        {
            var token = SenderToken;
            var client = UdpClient;
            var queue = PacketQueue;

            var synchronizerTicks = (double)Stopwatch.GetTimestamp();
            var synchronizerResolution = (Stopwatch.Frequency * 0.005);
            var tickResolution = 10_000_000.0 / Stopwatch.Frequency;
            Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Timer accuracy: {Stopwatch.Frequency.ToString("#,##0", CultureInfo.InvariantCulture)}/{synchronizerResolution.ToString(CultureInfo.InvariantCulture)} (high resolution? {Stopwatch.IsHighResolution})", DateTime.Now);

            while (!token.IsCancellationRequested)
            {
                var hasPacket = queue.TryDequeue(out var packet);

                byte[] packetArray = null;
                if (hasPacket)
                {
                    if (PlayingWait == null || PlayingWait.Task.IsCompleted)
                        PlayingWait = new TaskCompletionSource<bool>();

                    packetArray = packet.Bytes.ToArray();
                }

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

                var durationModifier = hasPacket ? packet.MillisecondDuration / 5 : 4;
                var cts = Math.Max(Stopwatch.GetTimestamp() - synchronizerTicks, 0);
                if (cts < synchronizerResolution * durationModifier)
                    await Task.Delay(TimeSpan.FromTicks((long)(((synchronizerResolution * durationModifier) - cts) * tickResolution))).ConfigureAwait(false);

                synchronizerTicks += synchronizerResolution * durationModifier;

                if (!hasPacket)
                    continue;

                SendSpeaking(true);
                await UdpClient.SendAsync(packetArray, packetArray.Length).ConfigureAwait(false);

                if (!packet.IsSilence && queue.Count == 0)
                {
                    var nullpcm = new byte[AudioFormat.CalculateSampleSize(20)];
                    for (var i = 0; i < 3; i++)
                    {
                        var nullpacket = new byte[nullpcm.Length];
                        var nullpacketmem = nullpacket.AsMemory();

                        PreparePacket(nullpcm, ref nullpacketmem);
                        EnqueuePacket(new VoicePacket(nullpacketmem, 20, true));
                    }
                }
                else if (queue.Count == 0)
                {
                    SendSpeaking(false);
                    PlayingWait?.SetResult(true);
                }
            }
        }

#if !NETSTANDARD1_1
        private bool ProcessPacket(ReadOnlySpan<byte> data, ref Memory<byte> opus, ref Memory<byte> pcm, IList<ReadOnlyMemory<byte>> pcmPackets, out AudioSender voiceSender, out AudioFormat outputFormat)
        {
            voiceSender = null;
            outputFormat = default;
            if (!Rtp.IsRtpHeader(data))
                return false;

            Rtp.DecodeHeader(data, out var sequence, out var timestamp, out var ssrc, out var hasExtension);

            var vtx = TransmittingSSRCs[ssrc];
            voiceSender = vtx;
            if (sequence <= vtx.LastSequence) // out-of-order packet; discard
                return false;
            var gap = vtx.LastSequence != 0 ? sequence - 1 - vtx.LastSequence : 0;

            if (gap >= 5)
                Discord.DebugLogger.LogMessage(LogLevel.Warning, "VNext RX", "5 or more voice packets were dropped when receiving", DateTime.Now);

            Span<byte> nonce = stackalloc byte[Sodium.NonceSize];
            Sodium.GetNonce(data, nonce, SelectedEncryptionMode);
            Rtp.GetDataFromPacket(data, out var encryptedOpus, SelectedEncryptionMode);

            var opusSize = Sodium.CalculateSourceSize(encryptedOpus);
            opus = opus.Slice(0, opusSize);
            var opusSpan = opus.Span;
            try
            {
                Sodium.Decrypt(encryptedOpus, opusSpan, nonce);

                // Strip extensions, if any
                if (hasExtension)
                {
                    // RFC 5285, 4.2 One-Byte header
                    // http://www.rfcreader.com/#rfc5285_line186
                    if (opusSpan[0] == 0xBE && opusSpan[1] == 0xDE)
                    {
                        var headerLen = opusSpan[2] << 8 | opusSpan[3];
                        var i = 4;
                        for (; i < headerLen + 4; i++)
                        {
                            var @byte = opusSpan[i];

                            // ID is currently unused since we skip it anyway
                            //var id = (byte)(@byte >> 4);
                            var length = (byte)(@byte & 0x0F) + 1;

                            i += length;
                        }

                        // Strip extension padding too
                        while (opusSpan[i] == 0)
                            i++;

                        opusSpan = opusSpan.Slice(i);
                    }

                    // TODO: consider implementing RFC 5285, 4.3. Two-Byte Header
                }

                if (gap == 1)
                {
                    var lastSampleCount = Opus.GetLastPacketSampleCount(vtx.Decoder);
                    var fecpcm = new byte[AudioFormat.SampleCountToSampleSize(lastSampleCount)];
                    var fecpcmMem = fecpcm.AsSpan();
                    Opus.Decode(vtx.Decoder, opusSpan, ref fecpcmMem, true, out _);
                    pcmPackets.Add(fecpcm.AsMemory(0, fecpcmMem.Length));
                }
                else if (gap > 1)
                {
                    var lastSampleCount = Opus.GetLastPacketSampleCount(vtx.Decoder);
                    for (var i = 0; i < gap; i++)
                    {
                        var fecpcm = new byte[AudioFormat.SampleCountToSampleSize(lastSampleCount)];
                        var fecpcmMem = fecpcm.AsSpan();
                        Opus.ProcessPacketLoss(vtx.Decoder, lastSampleCount, ref fecpcmMem);
                        pcmPackets.Add(fecpcm.AsMemory(0, fecpcmMem.Length));
                    }
                }

                var pcmSpan = pcm.Span;
                Opus.Decode(vtx.Decoder, opusSpan, ref pcmSpan, false, out outputFormat);
                pcm = pcm.Slice(0, pcmSpan.Length);
            }
            finally
            {
                vtx.LastSequence = sequence;
            }

            return true;
        }

        private async Task ProcessVoicePacket(byte[] data)
        {
            if (data.Length < 13) // minimum packet length
                return;

            try
            {
                var pcm = new byte[AudioFormat.CalculateMaximumFrameSize()];
                var pcmMem = pcm.AsMemory();
                var opus = new byte[pcm.Length];
                var opusMem = opus.AsMemory();
                var pcmFillers = new List<ReadOnlyMemory<byte>>();
                if (!ProcessPacket(data, ref opusMem, ref pcmMem, pcmFillers, out var vtx, out var audioFormat))
                    return;

                foreach (var pcmFiller in pcmFillers)
                    await _voiceReceived.InvokeAsync(new VoiceReceiveEventArgs(Discord)
                    {
                        SSRC = vtx.SSRC,
                        User = vtx.User,
                        PcmData = pcmFiller,
                        OpusData = new byte[0].AsMemory(),
                        AudioFormat = audioFormat,
                        AudioDuration = audioFormat.CalculateSampleDuration(pcmFiller.Length)
                    }).ConfigureAwait(false);

                await _voiceReceived.InvokeAsync(new VoiceReceiveEventArgs(Discord)
                {
                    SSRC = vtx.SSRC,
                    User = vtx.User,
                    PcmData = pcmMem,
                    OpusData = opusMem,
                    AudioFormat = audioFormat,
                    AudioDuration = audioFormat.CalculateSampleDuration(pcmMem.Length)
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Discord.DebugLogger.LogMessage(LogLevel.Error, "VNext RX", "Exception occured when decoding incoming audio data", DateTime.Now, ex);
            }
        }
#endif

        private void ProcessKeepalive(byte[] data)
        {
            try
            {
                var keepalive = BinaryPrimitives.ReadUInt64LittleEndian(data);

                if (!KeepaliveTimestamps.TryRemove(keepalive, out var timestamp))
                    return;

                var tdelta = (int)(((Stopwatch.GetTimestamp() - timestamp) / (double)Stopwatch.Frequency) * 1000);
                Volatile.Write(ref _wsPing, tdelta);
                Discord.DebugLogger.LogMessage(LogLevel.Debug, "VNext UDP", $"Received UDP keepalive {keepalive}, ping {tdelta}ms", DateTime.Now);
            }
            catch (Exception ex)
            {
                Discord.DebugLogger.LogMessage(LogLevel.Error, "VNext UDP", "Exception occured when handling keepalive", DateTime.Now, ex);
            }
        }

        private async Task UdpReceiverTask()
        {
            var token = ReceiverToken;
            var client = UdpClient;

            while (!token.IsCancellationRequested)
            {
                var data = await client.ReceiveAsync().ConfigureAwait(false);
                if (data.Length == 8)
                    ProcessKeepalive(data);
#if !NETSTANDARD1_1
                else if (Configuration.EnableIncoming)
                    await ProcessVoicePacket(data).ConfigureAwait(false);
#endif
            }
        }

        /// <summary>
        /// Sends a speaking status to the connected voice channel.
        /// </summary>
        /// <param name="speaking">Whether the current user is speaking or not.</param>
        /// <returns>A task representing the sending operation.</returns>
        public void SendSpeaking(bool speaking = true)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("The connection is not initialized");

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
        /// Gets a transmit stream for this connection, optionally specifying a packet size to use with the stream. If a stream is already configured, it will return the existing one.
        /// </summary>
        /// <param name="sampleDuration">Duration, in ms, to use for audio packets.</param>
        /// <returns>Transmit stream.</returns>
        public VoiceTransmitStream GetTransmitStream(int sampleDuration = 20)
        {
            if (!AudioFormat.AllowedSampleDurations.Contains(sampleDuration))
                throw new ArgumentOutOfRangeException(nameof(sampleDuration), "Invalid PCM sample duration specified.");

            if (TransmitStream == null)
                TransmitStream = new VoiceTransmitStream(this, sampleDuration);

            return TransmitStream;
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
            SenderTokenSource.Cancel();
#if !NETSTANDARD1_1
            if (Configuration.EnableIncoming)
                ReceiverTokenSource.Cancel();
#endif

            try
            {
                VoiceWs.DisconnectAsync(null).ConfigureAwait(false).GetAwaiter().GetResult();
                UdpClient.Close();
            }
            catch (Exception)
            { }

            Opus?.Dispose();
            Opus = null;
            Sodium?.Dispose();
            Sodium = null;
            Rtp?.Dispose();
            Rtp = null;

            if (VoiceDisconnected != null)
                VoiceDisconnected(Guild);
        }

        private async Task HeartbeatAsync()
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

        private async Task KeepaliveAsync()
        {
            await Task.Yield();

            var token = KeepaliveToken;
            var client = UdpClient;

            while (!token.IsCancellationRequested)
            {
                var timestamp = Stopwatch.GetTimestamp();
                var keepalive = Volatile.Read(ref _lastKeepalive);
                Volatile.Write(ref _lastKeepalive, keepalive + 1);
                KeepaliveTimestamps.TryAdd(keepalive, timestamp);

                var packet = new byte[8];
                BinaryPrimitives.WriteUInt64LittleEndian(packet, keepalive);

                await client.SendAsync(packet, packet.Length).ConfigureAwait(false);

                await Task.Delay(5000, token);
            }
        }

        private async Task Stage1(VoiceReadyPayload voiceReady)
        {
#if !NETSTANDARD1_1
            // IP Discovery
            UdpClient.Setup(ConnectionEndpoint);

            var pck = new byte[70];
            PreparePacket(pck);
            await UdpClient.SendAsync(pck, pck.Length).ConfigureAwait(false);

            var ipd = await UdpClient.ReceiveAsync().ConfigureAwait(false);
            ReadPacket(ipd, out var ip, out var port);
            DiscoveredEndpoint = new IPEndPoint(ip, port);
            Discord.DebugLogger.LogMessage(LogLevel.Debug, "VNext UDP", $"Endpoint discovery resulted in {ip}:{port}", DateTime.Now);

            void PreparePacket(byte[] packet)
            {
                var ssrc = SSRC;
                var packetSpan = packet.AsSpan();
                MemoryMarshal.Write(packetSpan, ref ssrc);
                Helpers.ZeroFill(packetSpan);
            }

            void ReadPacket(byte[] packet, out System.Net.IPAddress decodedIp, out ushort decodedPort)
            {
                var packetSpan = packet.AsSpan();

                var ipString = new UTF8Encoding(false).GetString(packet, 4, 64 /* 70 - 6 */).TrimEnd('\0');
                decodedIp = System.Net.IPAddress.Parse(ipString);

                decodedPort = BinaryPrimitives.ReadUInt16LittleEndian(packetSpan.Slice(68 /* 70 - 2 */));
            }
#else
            this.Discord.DebugLogger.LogMessage(LogLevel.Debug, "VNext UDP", $"Voice receive not supported - not performing endpoint discovery", DateTime.Now);
            await Task.Yield(); // just stop bothering me VS
#endif

            // Select voice encryption mode
            var selectedEncryptionMode = Sodium.SelectMode(voiceReady.Modes);
            SelectedEncryptionMode = selectedEncryptionMode.Value;

            // Ready
            Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Selected encryption mode: {selectedEncryptionMode.Key}", DateTime.Now);
            var vsp = new VoiceDispatch
            {
                OpCode = 1,
                Payload = new VoiceSelectProtocolPayload
                {
                    Protocol = "udp",
                    Data = new VoiceSelectProtocolPayloadData
                    {
#if !NETSTANDARD1_1
                        Address = DiscoveredEndpoint.Address.ToString(),
                        Port = (ushort)DiscoveredEndpoint.Port,
#else
                        Address = "0.0.0.0",
                        Port = 0,
#endif
                        Mode = selectedEncryptionMode.Key
                    }
                }
            };
            var vsj = JsonConvert.SerializeObject(vsp, Formatting.None);
            VoiceWs.SendMessage(vsj);

            SenderTokenSource = new CancellationTokenSource();
            SenderTask = Task.Run(VoiceSenderTask, SenderToken);

            ReceiverTokenSource = new CancellationTokenSource();
            ReceiverTask = Task.Run(UdpReceiverTask, ReceiverToken);
        }

        private Task Stage2(VoiceSessionDescriptionPayload voiceSessionDescription)
        {
            SelectedEncryptionMode = Sodium.SupportedModes[voiceSessionDescription.Mode.ToLowerInvariant()];
            Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Discord updated encryption mode: {SelectedEncryptionMode}", DateTime.Now);

            // start keepalive
            KeepaliveTokenSource = new CancellationTokenSource();
            KeepaliveTask = KeepaliveAsync();

            // send 3 packets of silence to get things going
            var nullpcm = new byte[AudioFormat.CalculateSampleSize(20)];
            for (var i = 0; i < 3; i++)
            {
                var nullopus = new byte[nullpcm.Length];
                var nullopusmem = nullopus.AsMemory();
                PreparePacket(nullpcm, ref nullopusmem);
                EnqueuePacket(new VoicePacket(nullopusmem, 20));
            }

            IsInitialized = true;
            ReadyWait.SetResult(true);

            return Task.Delay(0);
        }

        private async Task HandleDispatch(JObject jo)
        {
            var opc = (int)jo["op"];
            var opp = jo["d"] as JObject;

            switch (opc)
            {
                case 2: // READY
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP2 received", DateTime.Now);
                    var vrp = opp.ToObject<VoiceReadyPayload>();
                    SSRC = vrp.SSRC;
                    ConnectionEndpoint = new ConnectionEndpoint(ConnectionEndpoint.Hostname, vrp.Port);
                    // this is not the valid interval
                    // oh, discord
                    //this.HeartbeatInterval = vrp.HeartbeatInterval;
                    HeartbeatTask = Task.Run(HeartbeatAsync);
                    await Stage1(vrp).ConfigureAwait(false);
                    break;

                case 4: // SESSION_DESCRIPTION
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP4 received", DateTime.Now);
                    var vsd = opp.ToObject<VoiceSessionDescriptionPayload>();
                    Key = vsd.SecretKey;
                    Sodium = new DSharpPlus.VoiceNext.Codec.Sodium(Key.AsMemory());
                    await Stage2(vsd).ConfigureAwait(false);
                    break;

                case 5: // SPEAKING
                    // Don't spam OP5
                    //this.Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP5 received", DateTime.Now);
                    var spd = opp.ToObject<VoiceSpeakingPayload>();
                    var spk = new UserSpeakingEventArgs(Discord)
                    {
                        Speaking = spd.Speaking,
                        SSRC = spd.SSRC.Value,
                        User = Discord.InternalGetCachedUser(spd.UserId.Value)
                    };

#if !NETSTANDARD1_1
                    if (spk.User != null && TransmittingSSRCs.TryGetValue(spk.SSRC, out var txssrc5) && txssrc5.Id == 0)
                    {
                        txssrc5.User = spk.User;
                    }
                    else
                    {
                        var opus = Opus.CreateDecoder();
                        var vtx = new AudioSender(spk.SSRC, opus)
                        {
                            User = await Discord.GetUserAsync(spd.UserId.Value).ConfigureAwait(false)
                        };

                        if (!TransmittingSSRCs.TryAdd(spk.SSRC, vtx))
                            Opus.DestroyDecoder(opus);
                    }
#endif

                    await _userSpeaking.InvokeAsync(spk).ConfigureAwait(false);
                    break;

                case 6: // HEARTBEAT ACK
                    var dt = DateTime.Now;
                    var ping = (int)(dt - LastHeartbeat).TotalMilliseconds;
                    Volatile.Write(ref _wsPing, ping);
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", $"Received voice heartbeat ACK, ping {ping.ToString("#,##0", CultureInfo.InvariantCulture)}ms", dt);
                    LastHeartbeat = dt;
                    break;

                case 8: // HELLO
                    // this sends a heartbeat interval that we need to use for heartbeating
                    HeartbeatInterval = opp["heartbeat_interval"].ToObject<int>();
                    break;

                case 9: // RESUMED
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "VoiceNext", "OP9 received", DateTime.Now);
                    HeartbeatTask = Task.Run(HeartbeatAsync);
                    break;

                case 12: // CLIENT_CONNECTED
                    var ujpd = opp.ToObject<VoiceUserJoinPayload>();
                    var usrj = await Discord.GetUserAsync(ujpd.UserId).ConfigureAwait(false);

#if !NETSTANDARD1_1
                    {
                        var opus = Opus.CreateDecoder();
                        var vtx = new AudioSender(ujpd.SSRC, opus)
                        {
                            User = usrj
                        };

                        if (!TransmittingSSRCs.TryAdd(vtx.SSRC, vtx))
                            Opus.DestroyDecoder(opus);
                    }
#endif

                    await _userJoined.InvokeAsync(new VoiceUserJoinEventArgs(Discord) { User = usrj, SSRC = ujpd.SSRC }).ConfigureAwait(false);
                    break;

                case 13: // CLIENT_DISCONNECTED
                    var ulpd = opp.ToObject<VoiceUserLeavePayload>();

#if !NETSTANDARD1_1
                    var txssrc = TransmittingSSRCs.FirstOrDefault(x => x.Value.Id == ulpd.UserId);
                    if (TransmittingSSRCs.ContainsKey(txssrc.Key))
                    {
                        TransmittingSSRCs.TryRemove(txssrc.Key, out var txssrc13);
                        Opus.DestroyDecoder(txssrc13.Decoder);
                    }
#endif

                    var usrl = await Discord.GetUserAsync(ulpd.UserId).ConfigureAwait(false);
                    await _userLeft.InvokeAsync(new VoiceUserLeaveEventArgs(Discord)
                    {
                        User = usrl
#if !NETSTANDARD1_1
                        ,
                        SSRC = txssrc.Key
#endif
                    }).ConfigureAwait(false);
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
                VoiceWs.Disconnected += VoiceWS_SocketClosed;
                VoiceWs.MessageRecieved += VoiceWS_SocketMessage;
                VoiceWs.Connected += VoiceWS_SocketOpened;
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
// Alexa, how much is shipping to emzi
// NL -> PL is 18.50€ for packages <=2kg it seems (https://www.postnl.nl/en/mail-and-parcels/parcels/international-parcel/)