using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Models
{
    public class ChannelViewModel : PropertyChangedBase, IDisposable
    {
        private const int INITIAL_LOAD_LIMIT = 50;

        private DiscordChannel _channel;
        private DiscordUser _currentUser;
        private double _slowModeTimeout;
        private ConcurrentDictionary<ulong, CancellationTokenSource> _typingCancellation;
        private DateTimeOffset _typingLastSent;
        private DateTimeOffset _messageLastSent;
        private SynchronizationContext _context;
        private ResourceLoader _strings;
        private SemaphoreSlim _loadSemaphore;
        private DispatcherTimer _slowModeTimer;
        private bool _isTranscoding;

        public ChannelViewModel(DiscordChannel channel)
        {
            if (channel.Type == ChannelType.Voice)
                throw new InvalidOperationException("Unable to create a view model for a voice chanel"); // no op this

            _strings = ResourceLoader.GetForCurrentView("ChannelPage");
            _loadSemaphore = new SemaphoreSlim(1, 1);
            _context = SynchronizationContext.Current;
            _typingCancellation = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _typingLastSent = DateTime.Now - TimeSpan.FromSeconds(10);

            Channel = channel;
            CurrentUser = channel.Guild?.CurrentMember ?? App.Discord.CurrentUser;

            App.Discord.TypingStarted += OnTypingStarted;
            App.Discord.MessageCreated += OnMessageCreated;
            App.Discord.MessageDeleted += OnMessageDeleted;
            App.Discord.ChannelUpdated += OnChannelUpdated;
            App.Discord.Resumed += OnResumed;

            TypingUsers = new ObservableCollection<DiscordUser>();
            Messages = new ObservableCollection<DiscordMessage>();
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

            if (channel.Guild != null)
            {
                Permissions = _channel.PermissionsFor(channel.Guild.CurrentMember);
            }
            else
            {
                Permissions = Permissions.Administrator;
            }

            _slowModeTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1 / 30) };
            _slowModeTimer.Tick += (o, e) =>
            {
                SlowModeTimeout = Math.Max(0, PerUserRateLimit - (DateTimeOffset.Now - _messageLastSent).TotalMilliseconds);
                InvokePropertyChanged(nameof(ShowSlowModeTimeout));
                if (SlowModeTimeout == 0)
                {
                    InvokePropertyChanged(nameof(CanSend));
                    _slowModeTimer.Stop();
                }
            };
        }

        public ObservableCollection<DiscordMessage> Messages { get; set; }

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

        public ObservableCollection<DiscordUser> TypingUsers { get; set; }

        public ObservableCollection<FileUploadModel> FileUploads { get; set; }

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
                        return Strings.NaturalJoin(dm.Recipients.Values.Select(r => r.Username));
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
           CanSend || (SlowModeTimeout != 0 && !ImmuneToSlowMode) ? 
            string.Format(_strings.GetString("MessageChannelFormat"), ChannelPrefix, ChannelName) : 
            _strings.GetString("ChannelReadonlyText");

        public bool CanType => CanSend || (SlowModeTimeout != 0 && !ImmuneToSlowMode);

        /// <summary>
        /// The title for the page displaying the channel
        /// </summary>
        public string TitleText => Channel.Guild != null ? $"{FullChannelName} - {Channel.Guild.Name}" : FullChannelName;

        /// <summary>
        /// The icon to show in the top left of a channel
        /// </summary>
        public string UserImageUrl
        {
            get
            {
                if (Channel is DiscordDmChannel dm)
                {
                    if (dm.Type == ChannelType.Private && dm.Recipient != null)
                        return dm.Recipient.AvatarUrl;

                    if (dm.Type == ChannelType.Group && dm.IconUrl != null)
                        return dm.IconUrl;
                }

                return null;
            }
        }

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

        public double PerUserRateLimit =>
            (_channel.PerUserRateLimit ?? 0) * 1000;

        public bool ShowSlowModeTimeout =>
            SlowModeTimeout > 0;

        public double SlowModeTimeout { get => _slowModeTimeout; set => OnPropertySet(ref _slowModeTimeout, value); }

        // TODO: Componentise?
        public int UploadLimit => HasNitro ? 52_428_800 : 8_388_608;

        public ulong UploadSize => (ulong)FileUploads.Sum(u => (double)u.Length);

        public string DisplayUploadSize => (UploadSize / 1_048_576d).ToString("F2");

        public double UploadProgressBarValue => Math.Min(UploadSize, (ulong)UploadLimit);

        public Brush UploadInfoForeground => !CanSend ? (Brush)Application.Current.Resources["ErrorTextForegroundBrush"] : null;

        public string DisplayUploadLimit => (UploadLimit / 1_048_576d).ToString("F2");

        public bool ShowUploads => FileUploads.Any() || IsTranscoding;

        public bool ShowUserlistButton => Channel.Type == ChannelType.Group || Channel.Guild != null;

        public bool ShowCallButton => Channel.Type == ChannelType.Group || Channel.Type == ChannelType.Private;

        public bool HasNitro => Channel.Discord.CurrentUser.HasNitro;

        public bool ShowEditButton
            => Channel.Guild != null && Permissions.HasPermission(Permissions.ManageChannels);

        public bool ShowSlowMode
            => Channel.PerUserRateLimit.HasValue && Channel.PerUserRateLimit != 0;

        public string SlowModeText
            => string.Format(_strings.GetString(ImmuneToSlowMode ? "ImmuneSlowModeFormat" : "SlowModeFormat"), TimeSpan.FromSeconds(Channel.PerUserRateLimit ?? 0).ToNaturalString());

        private bool ImmuneToSlowMode => Permissions.HasPermission(Permissions.ManageMessages) && Permissions.HasPermission(Permissions.ManageChannels);

        public bool ShowTypingUsers
            => TypingUsers?.Any() == true;

        public bool ShowTypingContainer =>
            ShowSlowMode || ShowTypingUsers;

        public DateTimeOffset LastAccessed { get; internal set; }

        public bool IsEditMode { get; set; }

        public bool IsTranscoding
        {
            get => _isTranscoding;
            internal set
            {
                OnPropertySet(ref _isTranscoding, value);
                InvokePropertyChanged(nameof(ShowUploads));
            }
        }

        public bool ShowPopoutButton
            => WindowManager.IsMainWindow && WindowManager.MultipleWindowsSupported;

        public bool IsDisposed { get; internal set; }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            await _loadSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
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
            }
            finally
            {
                _loadSemaphore.Release();
            }

        }

        private Task OnMessageDeleted(MessageDeleteEventArgs e)
        {
            // BUGBUG: These are fire and forget, may be better to refactor into an async event setup?
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

        private async Task OnResumed(ReadyEventArgs e)
        {
            await _loadSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                // needs work, but it's functional
                // i also never want to touch it again

                var rawMessages = (await Channel.GetMessagesAsync().ConfigureAwait(false));
                var messages = rawMessages.Reverse().ToList();
                var lastMessage = messages.LastOrDefault(m => m.Id == Messages.LastOrDefault()?.Id);
                if (lastMessage != null)
                {
                    if (lastMessage.Id != messages.LastOrDefault().Id)
                    {
                        var messagesToAppend = messages.Skip(messages.IndexOf(lastMessage) + 1).Reverse().ToList();
                        InsertMessages(Messages.Count - 1, messagesToAppend);
                    }
                }
                else
                {
                    ClearAndAddMessages(messages);
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        internal async Task LoadMessagesAsync()
        {
            await _loadSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!Messages.Any())
                {
                    await UnsafeLoadMessages().ConfigureAwait(false);
                }
                else
                {
                    var message = Messages.Last();
                    var index = Messages.IndexOf(message);
                    var messages = await Channel.GetMessagesAfterAsync(message.Id, 100).ConfigureAwait(false);
                    if (messages.Count < 100)
                    {
                        InsertMessages(index, messages);
                    }
                    else
                    {
                        ClearAndAddMessages(await Channel.GetMessagesAsync());
                    }
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private void InsertMessages(int index, IEnumerable<DiscordMessage> messages) => _context.Post(d =>
        {
            var t = d as ChannelViewModel;
            t.RequestMissingUsers(messages);

            foreach (var mess in messages)
            {
                if (!t.Messages.Any(m => m.Id == mess.Id))
                {
                    t.Messages.Insert(index + 1, mess);
                }
            }
        }, this);

        private void ClearAndAddMessages(IEnumerable<DiscordMessage> messages) => _context.Post(d =>
        {
            var t = d as ChannelViewModel;
            t.Messages.Clear();

            t.RequestMissingUsers(messages);
            foreach (var message in messages.Reverse())
            {
                if (!t.Messages.Any(m => m.Id == message.Id))
                {
                    t.Messages.Add(message);
                }
            }
        }, this);

        private void RequestMissingUsers(IEnumerable<DiscordMessage> messages)
        {
            if (Channel.Guild != null)
            {
                var usersToSync = messages.Select(m => m.Author).OfType<DiscordMember>().Where(u => u.IsLocal);
                if (usersToSync.Any())
                    Channel.Guild.RequestUserPresences(usersToSync);
            }
        }

        private async Task UnsafeLoadMessages()
        {
            var messages = await Channel.GetMessagesAsync(INITIAL_LOAD_LIMIT).ConfigureAwait(false);
            if (messages.Any())
                ClearAndAddMessages(messages);
        }

        internal async Task LoadMessagesBeforeAsync()
        {
            await _loadSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var message = Messages.FirstOrDefault();
                if (message != null)
                {
                    var messages = await Channel.GetMessagesBeforeAsync(message.Id, INITIAL_LOAD_LIMIT).ConfigureAwait(false);
                    if (messages.Any())
                    {
                        InsertMessages(-1, messages);
                    }
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
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
                        if (FileUploads.Any())
                        {
                            var models = FileUploads.ToArray();
                            FileUploads.Clear();
                            var files = new Dictionary<string, IInputStream>();
                            foreach (var item in models)
                            {
                                files.Add(item.Spoiler ? $"SPOILER_{item.FileName}" : item.FileName, await item.GetStreamAsync().ConfigureAwait(false));
                            }

                            await Tools.SendFilesWithProgressAsync(Channel, str, files, progress).ConfigureAwait(false);

                            foreach (var item in files)
                            {
                                item.Value.Dispose();
                            }

                            foreach (var item in models)
                            {
                                if (item.IsTemporary && item.StorageFile != null)
                                {
                                    await item.StorageFile.DeleteAsync();
                                }

                                item.Dispose();
                            }
                        }
                        else
                        {
                            await Channel.SendMessageAsync(str).ConfigureAwait(false);
                        }

                        if (!ImmuneToSlowMode)
                        {
                            _messageLastSent = DateTimeOffset.Now;
                            SlowModeTimeout = PerUserRateLimit;
                            InvokePropertyChanged(nameof(CanSend));
                            _context.Post(o => ((DispatcherTimer)o).Start(), _slowModeTimer);
                        }
                    }
                    catch
                    {
                        // TODO: this is shite
                        await UIUtilities.ShowErrorDialogAsync(
                            "Failed to send message!",
                            "Oops, sending that didn't go so well, which probably means Discord is having a stroke. Again. Please try again later.");
                    }
                }
            }
        }

        #region Typing

        public async Task TriggerTypingAsync(string text)
        {
            if (!string.IsNullOrEmpty(text) && (DateTimeOffset.Now - _typingLastSent).Seconds > 10)
            {
                _typingLastSent = DateTimeOffset.Now;

                try
                {
                    await Channel.TriggerTypingAsync().ConfigureAwait(false);
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
                    TypingUsers.Add(Channel.Guild != null && Channel.Guild.Members.TryGetValue(e.User.Id, out var member) ? member : e.User);
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
            IsDisposed = true;

            if (Channel.Type != ChannelType.Voice)
            {
                App.Discord.TypingStarted -= OnTypingStarted;
                App.Discord.MessageCreated -= OnMessageCreated;
                App.Discord.MessageDeleted -= OnMessageDeleted;
                App.Discord.ChannelUpdated -= OnChannelUpdated;
                App.Discord.Resumed -= OnResumed;
            }

            Messages.Clear();

            foreach (var item in FileUploads)
            {
                item.Dispose();
            }
        }
    }
}
