using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.VoiceNext.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Unicord.Universal.Voice.Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DiscordClient _discord;
        private ObservableCollection<DiscordChannel> _channels;
        private TaskCompletionSource<VoiceStateUpdateEventArgs> _voiceStateUpdateCompletion;
        private TaskCompletionSource<VoiceServerUpdateEventArgs> _voiceServerUpdateCompletion;
        private ConcurrentDictionary<uint, Stream> _videoStreams;
        private DiscordChannel _channel;
        private VoiceClient _client;
        private SemaphoreSlim _semaphore;

        public MainPage()
        {
            InitializeComponent();
            _channels = new ObservableCollection<DiscordChannel>();
            _voiceStateUpdateCompletion = new TaskCompletionSource<VoiceStateUpdateEventArgs>();
            _voiceServerUpdateCompletion = new TaskCompletionSource<VoiceServerUpdateEventArgs>();
            _videoStreams = new ConcurrentDictionary<uint, Stream>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            list.ItemsSource = _channels;
            var vault = new PasswordVault();
            var token = "";
            try
            {
                var password = vault.FindAllByResource("Unicord_Token").FirstOrDefault(t => t.UserName == "Default");
                password.RetrievePassword();
                token = password.Password;
            }
            catch { }

            if (string.IsNullOrWhiteSpace(token))
            {
                var dialog = new TokenPromptDialog();
                if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                    return;
                token = dialog.Token;
                vault.Add(new PasswordCredential("Unicord_Token", "Default", token));
            }

            _discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.User
            });
            _discord.Ready += _discord_Ready;
            await _discord.ConnectAsync();
        }

        private async Task _discord_Ready(ReadyEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var channel in _discord.Guilds.Values.SelectMany(s => s.Channels.Values).Where(c => c.IsVoice))
                {
                    _channels.Add(channel);
                }
            });
        }

        private async void list_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is DiscordChannel channel)
            {
                _channel = channel;
                _discord.VoiceStateUpdated += OnVoiceStateUpdated;
                _discord.VoiceServerUpdated += OnVoiceServerUpdated;
                SendVoiceStateUpdate(channel.Id, channel.Guild.Id);

                var vstu = await _voiceStateUpdateCompletion.Task.ConfigureAwait(false);
                var vsru = await _voiceServerUpdateCompletion.Task.ConfigureAwait(false);

                var options = new VoiceClientOptions()
                {
                    ChannelId = channel.Id,
                    GuildId = channel.Guild.Id,
                    CurrentUserId = _discord.CurrentUser.Id,
                    Endpoint = vsru.Endpoint,
                    Token = vsru.VoiceToken,
                    SessionId = vstu.SessionId
                };

                _client = new VoiceClient(options);
                _client.Disconnected += Client_Disconnected;
                _client.VideoDataRecieved += Client_VideoDataRecieved;
                await _client.ConnectAsync();
            }
        }

        private void Client_Disconnected(object sender, bool e)
        {
            if (!e)
            {
                foreach (var item in _videoStreams.Values)
                {
                    item.Flush();
                    item.Dispose();
                }

                _videoStreams.Clear();
            }
        }

        private void Client_VideoDataRecieved(object sender, VideoEventArgs e)
        {
            var data = e.Data;
            var ssrc = e.SSRC;

            Debug.WriteLine(data.Length);

            if (data.Length > 32)
            {
                _semaphore.Wait();
                if (!_videoStreams.TryGetValue(ssrc, out var stream))
                {
                    var path = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, $"{ssrc}.h264");
                    stream = File.Create(path);
                    _videoStreams[ssrc] = stream;
                }

                stream.Write(data, 0, data.Length);
                stream.Flush();
                _semaphore.Release();
            }
        }

        private void SendVoiceStateUpdate(ulong? channel_id, ulong? guild_id)
        {
            var vsd = new VoiceGatewayPayload
            {
                OpCode = 4,
                Data = new VoiceStateUpdatePayload
                {
                    GuildId = guild_id,
                    ChannelId = channel_id,
                    Deafened = false,
                    Muted = false
                }
            };

            var vsj = JsonConvert.SerializeObject(vsd, Formatting.None);
            _discord._webSocketClient.SendMessage(vsj);
        }


        private Task OnVoiceStateUpdated(VoiceStateUpdateEventArgs e)
        {
            if (e.Channel == _channel && e.User == _discord.CurrentUser && !_voiceStateUpdateCompletion.Task.IsCompleted)
            {
                _voiceStateUpdateCompletion.SetResult(e);
            }

            return Task.CompletedTask;
        }

        private Task OnVoiceServerUpdated(VoiceServerUpdateEventArgs e)
        {
            if (e.Guild == _channel.Guild || e.Channel == _channel)
            {
                _voiceServerUpdateCompletion.SetResult(e);
                _discord.VoiceServerUpdated -= OnVoiceServerUpdated;
            }

            return Task.CompletedTask;
        }

    }
}
