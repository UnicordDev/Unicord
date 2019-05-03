using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Models
{
    public class ChannelViewModel : PropertyChangedBase, IDisposable
    {
        private DiscordChannel _channel;
        private DiscordUser _currentUser;
        private double _slowModeTimeout;
        private ConcurrentDictionary<ulong, CancellationTokenSource> _typingCancellation;
        private DateTimeOffset _typingLastSent;
        private DateTimeOffset _messageLastSent;
        private SynchronizationContext _context;
        private SemaphoreSlim _loadSemaphore;
        private DispatcherTimer _dispatcherTimer;
        private bool _isTranscoding;
        private bool _slim;

        public ChannelViewModel(DiscordChannel channel, bool slim = false)
        {
            _slim = slim;
            _loadSemaphore = new SemaphoreSlim(0, 1);
            _context = SynchronizationContext.Current;
            _typingCancellation = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _typingLastSent = DateTime.Now - TimeSpan.FromSeconds(10);

            Channel = channel;
            CurrentUser = channel.Guild?.CurrentMember ?? App.Discord.CurrentUser;
            if (channel.Type != ChannelType.Voice && !_slim)
            {
                App.Discord.TypingStarted += OnTypingStarted;
                App.Discord.MessageCreated += OnMessageCreated;
                App.Discord.MessageDeleted += OnMessageDeleted;
                App.Discord.ChannelUpdated += OnChannelUpdated;
                App.Discord.Resumed += OnResumed;

                TypingUsers = new ObservableCollection<DiscordUser>();
            }

            if (channel.Guild != null)
            {
                Permissions = _channel.PermissionsFor(channel.Guild.CurrentMember);
            }
            else
            {
                Permissions = Permissions.Administrator;
            }

            AvailableUsers = new ObservableCollection<DiscordUser> { CurrentUser };

            if (!_slim)
            {
                Messages = new ObservableCollection<DiscordMessage>();

                _dispatcherTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1 / 30) };
                _dispatcherTimer.Tick += (o, e) =>
                {
                    SlowModeTimeout = Math.Max(0, PerUserRateLimit - (DateTimeOffset.Now - _messageLastSent).TotalMilliseconds);
                    InvokePropertyChanged(nameof(ShowSlowModeTimeout));
                    if (SlowModeTimeout == 0)
                    {
                        InvokePropertyChanged(nameof(CanSend));
                        _dispatcherTimer.Stop();
                    }
                };
            }

            FileUploads = new ObservableCollection<FileUploadModel>();
            FileUploads.CollectionChanged += (o, e) =>
            {
                InvokePropertyChanged(nameof(DisplayUploadSize));
                InvokePropertyChanged(nameof(UploadProgressBarValue));
                InvokePropertyChanged(nameof(CanSend));
                InvokePropertyChanged(nameof(UploadInfoForeground));
                InvokePropertyChanged(nameof(CanUpload));
                InvokePropertyChanged(nameof(ShowUploads));
            };

            _loadSemaphore.Release();
        }

        public ObservableCollection<DiscordMessage> Messages { get; set; }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            await _loadSemaphore.WaitAsync();

            if (e.Channel.Id == Channel.Id)
            {
                if (_typingCancellation.TryGetValue(e.Author.Id, out var src))
                {
                    src.Cancel();
                }

                var usr = TypingUsers.FirstOrDefault(u => u.Id == e.Author.Id);
                if (usr != null)
                {
                    _context.Post(a =>
                    {
                        TypingUsers.Remove(usr);
                        UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                    }, null);
                }
                if (!Messages.Any(m => m.Id == e.Message.Id))
                {
                    _context.Post(a => Messages.Add(e.Message), null);
                }
            }

            _loadSemaphore.Release();
        }

        private Task OnMessageDeleted(MessageDeleteEventArgs e)
        {
            _context.Post(a => Messages.Remove(e.Message), null);
            return Task.CompletedTask;
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

        private Task OnResumed(ReadyEventArgs e)
        {
            return LoadMessagesAsync();
        }

        public DiscordChannel Channel
        {
            get => _channel;
            set
            {
                OnPropertySet(ref _channel, value);

                if (_channel.Guild != null)
                {
                    Permissions = _channel.PermissionsFor(_channel.Guild.CurrentMember);
                }
                else
                {
                    Permissions = Permissions.Administrator;
                }

                InvokePropertyChanged(string.Empty);
            }
        }

        public DiscordUser CurrentUser
        {
            get => _currentUser;
            set
            {
                OnPropertySet(ref _currentUser, value);

                if (_channel.Guild != null)
                {
                    Permissions = _channel.PermissionsFor(_currentUser as DiscordMember);
                }
                else
                {
                    Permissions = Permissions.Administrator;
                }

                InvokePropertyChanged(string.Empty);
            }
        }

        public Permissions Permissions { get; set; }

        public DiscordUser Recipient => Channel.Type == ChannelType.Private ? (Channel as DiscordDmChannel).Recipient : null;

        public ObservableCollection<DiscordUser> AvailableUsers { get; set; }

        public ObservableCollection<DiscordUser> TypingUsers { get; set; }

        public string Topic => Channel.Topic != null ? Channel.Topic.Replace(new[] { "\r", "\n" }, " ").Truncate(512, "...") : string.Empty;

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
                {
                    return Channel.Name;
                }

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

        internal async Task LoadMessagesAsync()
        {
            if (_slim)
            {
                throw new InvalidOperationException("Unable to load messages with a slim view model.");
            }

            await _loadSemaphore.WaitAsync();

            if (!Messages.Any())
            {
                await UnsafeLoadMessages();
            }
            else
            {
                var message = Messages.Last();
                var index = Messages.IndexOf(message);
                var messages = await Channel.GetMessagesAfterAsync(message.Id, 100);
                if (messages.Count < 100)
                {
                    _context.Post(d =>
                    {
                        lock (Messages)
                        {
                            foreach (var mess in messages)
                            {
                                if (!Messages.Any(m => m.Id == mess.Id))
                                {
                                    Messages.Insert(index + 1, mess);
                                }
                            }
                        }

                    }, null);
                }
                else
                {
                    Messages.Clear();

                    await UnsafeLoadMessages();
                }
            }

            _loadSemaphore.Release();
        }

        private async Task UnsafeLoadMessages()
        {
            if (_slim)
            {
                throw new InvalidOperationException("Unable to load messages with a slim view model.");
            }

            var messages = await Channel.GetMessagesAsync(50).ConfigureAwait(false);
            if (messages.Any())
            {
                _context.Post(d =>
                {
                    lock (Messages)
                    {
                        foreach (var message in messages.Reverse())
                        {
                            if (!Messages.Any(m => m.Id == message.Id))
                            {
                                Messages.Add(message);
                            }
                        }
                    }

                }, null);
            }
        }

        internal async Task LoadMessagesBeforeAsync()
        {
            if (_slim)
            {
                throw new InvalidOperationException("Unable to load messages with a slim view model.");
            }

            await _loadSemaphore.WaitAsync();

            var message = Messages.FirstOrDefault();
            if (message != null)
            {
                var messages = await Channel.GetMessagesBeforeAsync(message.Id, 50).ConfigureAwait(false);
                if (messages.Any())
                {
                    _context.Post(d =>
                    {
                        lock (Messages)
                        {
                            foreach (var m in messages)
                            {
                                if (!Messages.Any(me => me.Id == m.Id))
                                {
                                    Messages.Insert(0, m);
                                }
                            }
                        }
                    }, null);
                }
            }

            _loadSemaphore.Release();
        }

        /// <summary>
        /// The full channel name (i.e. @WamWooWam#6402, #general, etc.)
        /// </summary>
        public string FullChannelName =>
            $"{ChannelPrefix}{ChannelName}{ChannelSuffix}";

        /// <summary>
        /// The current placeholder to display in the message text box
        /// </summary>
        public string ChannelPlaceholder =>
           CanSend || (SlowModeTimeout != 0 && !ImmuneToSlowMode) ? $"Message {ChannelPrefix}{ChannelName}" : "This channel is read-only";

        public bool CanType => CanSend || (SlowModeTimeout != 0 && !ImmuneToSlowMode);

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
                if (SlowModeTimeout != 0 && !ImmuneToSlowMode)
                {
                    return false;
                }

                if (UploadSize > (ulong)UploadLimit)
                {
                    return false;
                }

                if (Channel.Type == ChannelType.Voice)
                {
                    return false;
                }

                if (Channel is DiscordDmChannel)
                {
                    return true;
                }

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
                if (!CanSend)
                {
                    return false;
                }

                if (Channel is DiscordDmChannel)
                {
                    return true;
                }

                if (_currentUser is DiscordMember member)
                {
                    return Permissions.HasPermission(Permissions.AttachFiles);
                }

                return false;
            }
        }


        public double PerUserRateLimit => (_channel.PerUserRateLimit ?? 0) * 1000;

        public Visibility ShowSlowModeTimeout => SlowModeTimeout > 0 ? Visibility.Visible : Visibility.Collapsed;

        public double SlowModeTimeout { get => _slowModeTimeout; set => OnPropertySet(ref _slowModeTimeout, value); }

        public ObservableCollection<FileUploadModel> FileUploads { get; set; }

        public int UploadLimit => HasNitro ? 50 * 1024 * 1024 : 8 * 1024 * 1024;

        public ulong UploadSize => (ulong)FileUploads.Sum(u => (double)u.Length);

        public string DisplayUploadSize => (UploadSize / (1024d * 1024d)).ToString("F2");

        public double UploadProgressBarValue => Math.Min(UploadSize, (ulong)UploadLimit);

        public Brush UploadInfoForeground => !CanSend ? (Brush)Application.Current.Resources["ErrorTextForegroundBrush"] : null;

        public string DisplayUploadLimit => (UploadLimit / (1024d * 1024d)).ToString("F2");

        public Visibility ShowUploads => FileUploads.Any() || IsTranscoding ? Visibility.Visible : Visibility.Collapsed;

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
            {
                return;
            }

            if ((!string.IsNullOrWhiteSpace(textBox.Text) || FileUploads.Any()) && CanSend)
            {
                var str = textBox.Text;
                if (str.Length < 2000)
                {
                    textBox.Text = "";

                    try
                    {
                        var client = GetClient();

                        if (FileUploads.Any())
                        {
                            var models = FileUploads.ToArray();
                            FileUploads.Clear();
                            var files = models.ToDictionary(d => d.Spoiler ? $"SPOILER_{d.FileName}" : d.FileName, e => e.File);
                            await Tools.SendFilesWithProgressAsync(Channel, client, str, files, progress);

                            OnMessageSend();

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

                        if (client is DiscordRestClient rest)
                        {
                            await rest.CreateMessageAsync(Channel.Id, str, false, null);
                            OnMessageSend();

                            return;
                        }

                        await Channel.SendMessageAsync(str);
                        OnMessageSend();
                    }
                    catch
                    {
                        await UIUtilities.ShowErrorDialogAsync(
                            "Failed to send message!",
                            "Oops, sending that didn't go so well, which probably means Discord is having a stroke. Again. Please try again later.");
                    }
                }
            }
        }

        private void OnMessageSend()
        {
            if (!ImmuneToSlowMode)
            {
                _messageLastSent = DateTimeOffset.Now;
                SlowModeTimeout = PerUserRateLimit;
                InvokePropertyChanged(nameof(CanSend));
                _dispatcherTimer.Start();
            }
        }

        public Visibility ShowSlowMode
            => Channel.PerUserRateLimit.HasValue && Channel.PerUserRateLimit != 0 ? Visibility.Visible : Visibility.Collapsed;

        public string SlowModeText
            => $"Messages can be sent every " +
            $"{TimeSpan.FromSeconds(Channel.PerUserRateLimit.GetValueOrDefault()).ToNaturalString()}!" +
            (ImmuneToSlowMode ? " But, you're immune!" : "");

        private bool ImmuneToSlowMode => Permissions.HasPermission(Permissions.ManageMessages) && Permissions.HasPermission(Permissions.ManageChannels);

        public Visibility ShowTypingUsers
            => TypingUsers?.Any() == true ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowTypingContainer => ShowSlowMode == Visibility.Visible || ShowTypingUsers == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;

        public DateTimeOffset LastAccessed { get; internal set; }

        public bool IsEditMode { get; set; }

        public bool IsTranscoding
        {
            get => _isTranscoding;
            internal set
            {
                InvokePropertyChanged(nameof(ShowUploads));
                _isTranscoding = value;
            }
        }

        public async Task UpdateAvailableUsers()
        {
            try
            {
                AvailableUsers.Clear();
                AvailableUsers.Add(CurrentUser);

                if (Channel.Guild == null) // for now
                {
                    return;
                }

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

        #region Typing

        public async Task TriggerTypingAsync(string text)
        {
            if (!string.IsNullOrWhiteSpace(text) && (DateTimeOffset.Now - _typingLastSent).Seconds > 10)
            {
                _typingLastSent = DateTimeOffset.Now;

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

        private Task OnTypingStarted(TypingStartEventArgs e)
        {
            if (e.Channel.Id == Channel.Id && e.User.Id != CurrentUser.Id)
            {
                if (_typingCancellation.TryRemove(e.User.Id, out var src))
                {
                    src.Cancel();
                }

                _context.Post(o =>
                {
                    TypingUsers.Add(e.User);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                }, null);

                new Task(async () => await HandleTypingStartAsync(e)).Start();
            }

            return Task.CompletedTask;
        }

        private async Task HandleTypingStartAsync(TypingStartEventArgs e)
        {
            var source = new CancellationTokenSource();
            _typingCancellation[e.User.Id] = source;

            await Task.Delay(10_000, source.Token).ContinueWith(t =>
            {
                _context.Post(o =>
                {
                    TypingUsers.Remove(e.User);
                    UnsafeInvokePropertyChange(nameof(ShowTypingUsers));
                }, null);
            });
        }

        #endregion

        public virtual void Dispose()
        {
            if (Channel.Type != ChannelType.Voice && !_slim)
            {
                App.Discord.TypingStarted -= OnTypingStarted;
                App.Discord.MessageCreated -= OnMessageCreated;
                App.Discord.MessageDeleted -= OnMessageDeleted;
                App.Discord.ChannelUpdated -= OnChannelUpdated;
                App.Discord.Resumed -= OnResumed;
            }

            foreach (var item in FileUploads)
            {
                item.Dispose();
            }
        }
    }
}
