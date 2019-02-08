using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Unicord.Universal.Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DiscordClient _client;
        private VoiceNextExtension _vnext;
        private VoiceNextConnection _connection;
        private WasapiOutRT _audioOut;
        private MixingWaveProvider32 _mixer;
        private static WaveFormat _waveFormat = new WaveFormat(48000, 2);
        private ConcurrentDictionary<uint, BufferedWaveProvider> _userBuffers
            = new ConcurrentDictionary<uint, BufferedWaveProvider>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _client = new DiscordClient(new DiscordConfiguration()
            {
                Token = "MzE3ODMzOTkwMjMxMDMxODE4.DxJdKg.2evGMs3EFAhvy08PBeoNvESgo2M",
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });

            _client.DebugLogger.LogMessageReceived += (o, ev) => Debug.WriteLine(ev);
            _client.GuildAvailable += _client_GuildAvailable;

            _vnext = _client.UseVoiceNext(new VoiceNextConfiguration() { EnableIncoming = true });
            await _client.ConnectAsync();
        }

        private async Task _client_GuildAvailable(GuildCreateEventArgs e)
        {
            var chans = e.Guild.Channels.Where(c => c.Type == ChannelType.Voice).ToArray();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                foreach (var item in chans)
                {
                    list.Items.Add(item);
                }
            });
        }

        private async void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is DiscordChannel channel)
            {
                _connection = await _vnext.ConnectAsync(channel);

                _audioOut = new WasapiOutRT(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100);

                _mixer = new MixingWaveProvider32();
                _mixer.AddInputStream(new Wave16ToFloatProvider(new SilenceProvider(_waveFormat)));

                await _audioOut.Init(new WaveFloatTo16Provider(_mixer));
                _audioOut.Play();

                _connection.VoiceReceived += Connection_VoiceReceived;

                await _connection.SendAsync(Enumerable.Repeat((byte)0, 3840).ToArray(), 3840);

            }
        }

        private Task Connection_VoiceReceived(VoiceReceiveEventArgs e)
        {
            if (!(_userBuffers.TryGetValue(e.SSRC, out var provider)))
            {
                provider = new BufferedWaveProvider(_waveFormat) { BufferDuration = TimeSpan.FromMilliseconds(100), DiscardOnBufferOverflow = true };
                _userBuffers[e.SSRC] = provider;
                _mixer.AddInputStream(provider);
            }

            var voice = new byte[e.VoiceLength * 2];
            Buffer.BlockCopy(e.Voice.ToArray(), 0, voice, 0, e.VoiceLength);

            provider.AddSamples(voice, 0, e.VoiceLength * 2);

            return Task.CompletedTask;
        }
    }
}
