using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;
using DSharpPlus.EventArgs;
using System.Threading;
using System.Collections.Concurrent;
using Unicord.Abstractions;
using Windows.UI.Core;
#if WINDOWS_WPF
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
#elif WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

#if WINDOWS_WPF
namespace Unicord.Desktop.Models
#elif WINDOWS_UWP
namespace Unicord.Universal.Models
#endif
{
    public class ChannelViewModel : PropertyChangedBase, IDisposable
    {
        private DiscordChannel _channel;
        private DiscordUser _currentUser;

        private ConcurrentDictionary<ulong, CancellationTokenSource> _typingCancellation = new ConcurrentDictionary<ulong, CancellationTokenSource>();
        private DateTime _typingLastSent;

        public ChannelViewModel(DiscordChannel channel)
        {
            Channel = channel;
            CurrentUser = channel.Guild?.CurrentMember ?? App.Discord.CurrentUser;
            _typingLastSent = DateTime.Now - TimeSpan.FromSeconds(10);

            if (channel.Type != ChannelType.Voice)
            {
                App.Discord.TypingStarted += Discord_TypingStarted;
                App.Discord.MessageCreated += OnMessageCreated;
                App.Discord.ChannelUpdated += OnChannelUpdated;

                TypingUsers = new ObservableCollection<DiscordUser>();
            }

            if (channel.Guild != null)
                Permissions = _channel.PermissionsFor(channel.Guild.CurrentMember);
            else
                Permissions = Permissions.Administrator;

            AvailableUsers = new ObservableCollection<DiscordUser> { CurrentUser };

#if WINDOWS_UWP
            FileUploads = new ObservableCollection<FileUploadModel>();
            FileUploads.CollectionChanged += (o, e) =>
            {
                InvokePropertyChanged(nameof(DisplayUploadSize));
                InvokePropertyChanged(nameof(UploadProgressBarValue));
                InvokePropertyChanged(nameof(CanSend));
                InvokePropertyChanged(nameof(UploadInfoForeground));
                InvokePropertyChanged(nameof(CanUpload));
            };
#endif
        }

        private Task OnChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (e.ChannelAfter.Id == _channel.Id)
            {
                Channel = e.ChannelAfter;
                InvokePropertyChanged(string.Empty);
            }

            return Task.CompletedTask;
        }

#if WINDOWS_UWP

        private CoreDispatcher _dispatcher;
        public ChannelViewModel(DiscordChannel channel, CoreDispatcher dispatcher) : this(channel)
        {
            _dispatcher = dispatcher;
        }

        public ObservableCollection<FileUploadModel> FileUploads { get; set; }

#elif WINDOWS_WPF

        private Dispatcher _dispatcher;
        public ChannelViewModel(DiscordChannel channel, Dispatcher dispatcher) : this(channel)
        {
            _dispatcher = dispatcher;
        }
#endif

        public DiscordChannel Channel
        {
            get => _channel;
            set
            {
                OnPropertySet(ref _channel, value);

                if (_channel.Guild != null)
                    Permissions = _channel.PermissionsFor(_channel.Guild.CurrentMember);
                else
                    Permissions = Permissions.Administrator;

                InvokePropertyChanged(nameof(ChannelPrefix));
                InvokePropertyChanged(nameof(ChannelName));
                InvokePropertyChanged(nameof(ChannelSuffix));
                InvokePropertyChanged(nameof(ShowUsersButton));
            }
        }

        public DiscordUser CurrentUser
        {
            get => _currentUser;
            set
            {
                OnPropertySet(ref _currentUser, value);

                if (_channel.Guild != null)
                    Permissions = _channel.PermissionsFor(_currentUser as DiscordMember);
                else
                    Permissions = Permissions.Administrator;

                InvokePropertyChanged(nameof(HasNitro));
                InvokePropertyChanged(nameof(CanUpload));
            }
        }

        public ObservableCollection<DiscordUser> AvailableUsers { get; set; }

        public string Topic => Channel.Topic != null ? Channel.Topic.Replace(new[] { "\r", "\n" }, " ") : string.Empty;

        /// <summary>
        /// The channel's symbol. (i.e. #, @ etc.)
        /// </summary>
        public string ChannelPrefix =>
            Channel.Guild != null ? "#"
            : Channel.Type == ChannelType.Private ? "@"
            : string.Empty;

        /// <summary>
        /// The actual channel name. (i.e. general, WamWooWam, etc.)
        /// </summary>
        public string ChannelName
        {
            get
            {
                if (!string.IsNullOrEmpty(Channel.Name))
                    return Channel.Name;

                if (Channel is DiscordDmChannel dm)
                {
                    if (dm.Type == ChannelType.Private)
                    {
                        return dm.Recipient.Username;
                    }
                    else
                    {
                        return Strings.NaturalJoin(dm.Recipients.Select(r => r.Username));
                    }
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The suffix of the channel's display name. (i.e. #6402)
        /// </summary>
        public string ChannelSuffix =>
            Channel is DiscordDmChannel dm && dm.Type == ChannelType.Private ? $"#{dm.Recipient.Discriminator}" : string.Empty;

        /// <summary>
        /// The full channel name (i.e. @WamWooWam#6402, #general, etc.)
        /// </summary>
        public string FullChannelName =>
            $"{ChannelPrefix}{ChannelName}{ChannelSuffix}";

        /// <summary>
        /// The current placeholder to display in the message text box
        /// </summary>
        public string ChannelPlaceholder =>
           CanSend ? $"Message {ChannelPrefix}{ChannelName}" : "This channel is read-only";

        /// <summary>
        /// The title for the page displaying the channel
        /// </summary>
        public string TitleText => Channel.Guild != null ? $"{FullChannelName} - {Channel.Guild.Name}" : FullChannelName;

        public Visibility ShowUsersButton =>
            AvailableUsers.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowUserImage =>
            _channel is DiscordDmChannel c && c.Type == ChannelType.Private ? Visibility.Visible : Visibility.Collapsed;

        public string UserImageUrl =>
            _channel is DiscordDmChannel c && c.Type == ChannelType.Private ? c.Recipient?.NonAnimatedAvatarUrl : null;

        public bool CanSend
        {
            get
            {
#if WINDOWS_UWP
                if (UploadSize > (ulong)UploadLimit)
                    return false;
#endif

                if (Channel.Type == ChannelType.Voice)
                    return false;

                if (Channel is DiscordDmChannel)
                    return true;

                if (_currentUser is DiscordMember member)
                {
                    return Permissions.HasPermission(Permissions.SendMessages) && Permissions.HasPermission(Permissions.AccessChannels);
                }

                return false;
            }
        }

        public bool CanUpload
        {
            get
            {
#if WINDOWS_UWP
                if (UploadSize > (ulong)UploadLimit)
                    return false;
#endif
                if (Channel.Type == ChannelType.Voice)
                    return false;

                if (_channel is DiscordDmChannel)
                    return true;

                if (_currentUser is DiscordMember member)
                {
                    var perms = Permissions;
                    return perms.HasPermission(Permissions.AccessChannels) && perms.HasPermission(Permissions.SendMessages) && perms.HasPermission(Permissions.AttachFiles);
                }

                return false;
            }
        }

        public int UploadLimit => HasNitro ? 50 * 1024 * 1024 : 8 * 1024 * 1024;

#if WINDOWS_UWP

        public ulong UploadSize => (ulong)FileUploads.Sum(u => (double)u.Length);

        public string DisplayUploadSize => (UploadSize / (1024d * 1024d)).ToString("F2");

        public double UploadProgressBarValue => Math.Min(UploadSize, (ulong)UploadLimit);

        public Windows.UI.Xaml.Media.Brush UploadInfoForeground => !CanSend ? new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red) : null;

#endif

        public string DisplayUploadLimit => (UploadLimit / (1024d * 1024d)).ToString("F2");

        public Visibility ShowSendButton => CanSend ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowUserlistButton => Channel.Type == ChannelType.Group || Channel.Guild != null ? Visibility.Visible : Visibility.Collapsed;

        public bool HasNitro => GetClient().CurrentUser.HasNitro;

        public BaseDiscordClient GetClient()
        {
            if (App.AdditionalUserClients.TryGetValue(CurrentUser.Id, out var client))
            {
                return client;
            }

            return App.Discord;
        }

        /// <summary>
        /// Abstracts sending a message.
        /// </summary>
        /// <returns></returns>
        public async Task SendMessageAsync(TextBox textBox, IProgress<double?> progress = null)
        {
            if (Channel.Type == ChannelType.Voice)
                return;

            if (!string.IsNullOrWhiteSpace(textBox.Text)
#if WINDOWS_UWP
                || FileUploads.Any()
#endif
                )
            {
                var str = textBox.Text;
                if (str.Length < 2000)
                {
                    textBox.Text = "";

                    try
                    {

                        var client = GetClient();
#if WINDOWS_UWP
                        if (FileUploads.Any())
                        {
                            var models = FileUploads.ToArray();
                            FileUploads.Clear();
                            var files = models.ToDictionary(d => d.FileName, e => e.File);
                            await Tools.SendFilesWithProgressAsync(Channel, client, str, files, progress);

                            foreach (var model in models)
                            {
                                model.Dispose();

                                if (model.StorageFile != null && model.IsTemporary)
                                {
                                    await model.StorageFile.DeleteAsync();
                                }
                            }

                            return;
                        }
#endif
                        if (client is DiscordRestClient rest)
                        {
                            await rest.CreateMessageAsync(Channel.Id, str, false, null);
                            return;
                        }

                        await Channel.SendMessageAsync(str);
                    }
                    catch
                    {
                        await UIAbstractions.Current.ShowFailureDialogAsync(
                            "Failed to send!",
                            "Failed to send message!",
                            "Oops, sending that didn't go so well, which probably means Discord is having a stroke. Again. Please try again later.");
                    }
                }
            }
        }

        public Visibility ShowSlowMode
            => Channel.PerUserRateLimit.HasValue && Channel.PerUserRateLimit != 0 ? Visibility.Visible : Visibility.Collapsed;

        public string SlowModeText
            => $"Messages can be sent every " +
            $"{TimeSpan.FromSeconds(Channel.PerUserRateLimit.GetValueOrDefault()).ToNaturalString()}!" +
            (Permissions.HasPermission(Permissions.ManageMessages) && Permissions.HasPermission(Permissions.ManageChannels) ? " But, you're immune!" : "");

        public Visibility ShowTypingUsers
            => TypingUsers?.Any() == true ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowTypingContainer => ShowSlowMode == Visibility.Visible || ShowTypingUsers == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;

        public async Task UpdateAvailableUsers()
        {
            try
            {
                AvailableUsers.Clear();
                AvailableUsers.Add(CurrentUser);

                if (Channel.Guild == null) // for now
                    return;

                foreach (var u in App.AdditionalUserClients)
                {
                    var guild = u.Value.Guilds.FirstOrDefault(g => g.Key == Channel.GuildId).Value;
                    if (guild != null)
                    {
                        var m = await Channel.Guild.GetMemberAsync(u.Key);
                        if (Permissions.HasPermission(Permissions.AccessChannels) && Permissions.HasPermission(Permissions.SendMessages))
                        {
                            AvailableUsers.Add(m);
                        }
                    }
                }
            }
            finally
            {
                InvokePropertyChanged(nameof(ShowUsersButton));
                InvokePropertyChanged(nameof(AvailableUsers));
            }
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (_typingCancellation.TryGetValue(e.Author.Id, out var src))
                src.Cancel();

            var usr = TypingUsers.FirstOrDefault(u => u.Id == e.Author.Id);
            if (usr != null)
            {
#if WINDOWS_UWP
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TypingUsers.Remove(usr);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                });
#elif WINDOWS_WPF
                await _dispatcher.InvokeAsync(() =>
                {
                    TypingUsers.Remove(usr);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                });
#endif
            }
        }

        #region Typing

        public async Task TriggerTypingAsync(string text)
        {
            if (!string.IsNullOrWhiteSpace(text) && (DateTime.Now - _typingLastSent).Seconds > 10)
            {
                _typingLastSent = DateTime.Now;

                try
                {
                    var client = GetClient();
                    if (client is DiscordRestClient rest)
                    {
                        await rest.TriggerTypingAsync(Channel.Id);
                        return;
                    }
                    else
                    {
                        await Channel.TriggerTypingAsync();
                    }
                }
                catch { }
            }
        }

        public ObservableCollection<DiscordUser> TypingUsers { get; set; }
        public Permissions Permissions { get; set; }

        private async Task Discord_TypingStarted(TypingStartEventArgs e)
        {
            if (e.Channel.Id == Channel.Id && e.User.Id != CurrentUser.Id)
            {
                if (_typingCancellation.TryRemove(e.User.Id, out var src))
                    src.Cancel();

#if WINDOWS_UWP
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TypingUsers.Add(e.User);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                });
#elif WINDOWS_WPF
                await _dispatcher.InvokeAsync(() =>
                {
                    TypingUsers.Add(e.User);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                });
#endif

                new Task(async () => await HandleTypingStartAsync(e)).Start();
            }
        }

        private async Task HandleTypingStartAsync(TypingStartEventArgs e)
        {
            var source = new CancellationTokenSource();
            _typingCancellation[e.User.Id] = source;

            await Task.Delay(10_000, source.Token).ContinueWith(async t =>
            {
#if WINDOWS_UWP
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TypingUsers.Remove(e.User);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                });
#elif WINDOWS_WPF
                await _dispatcher.InvokeAsync(() =>
                {
                    TypingUsers.Remove(e.User);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                });
#endif
            });
        }

        #endregion

        public virtual void Dispose()
        {
            if (Channel.Type != ChannelType.Voice)
            {
                App.Discord.TypingStarted -= Discord_TypingStarted;
                App.Discord.MessageCreated -= OnMessageCreated;
            }
        }
    }
}
