using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Models
{
    public class ChannelViewModel : NotifyPropertyChangeImpl, IDisposable
    {
        private const int INITIAL_LOAD_LIMIT = 50;

        private DiscordChannel _channel;
        private DiscordUser _currentUser;
        private double _slowModeTimeout;
        private ConcurrentDictionary<ulong, CancellationTokenSource> _typingCancellation;
        private DateTimeOffset _typingLastSent;
        private DateTimeOffset _messageLastSent;
        private SynchronizationContext _context;
        private WindowHandle _windowHandle;
        private ResourceLoader _strings;
        private SemaphoreSlim _loadSemaphore;
        private DispatcherTimer _slowModeTimer;
        private string _messageText;
        private bool _isTranscoding;
        private ObservableCollection<DiscordMessage> _messages;
        private DiscordMessage _replyTo;
        private bool _replyPing = true;

        public ChannelViewModel(DiscordChannel channel, WindowHandle window)
        {
            if (channel.Type == ChannelType.Voice)
                throw new InvalidOperationException("Unable to create a view model for a voice chanel"); // no op this

            _windowHandle = window;
            _strings = ResourceLoader.GetForCurrentView("ChannelPage");
            _loadSemaphore = new SemaphoreSlim(1, 1);
            _context = SynchronizationContext.Current;
            _typingCancellation = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _typingLastSent = DateTime.Now - TimeSpan.FromSeconds(10);

            _channel = channel;
            _currentUser = channel.Guild?.CurrentMember ?? App.Discord.CurrentUser;
            _messages = new ObservableCollection<DiscordMessage>();

            App.Discord.TypingStarted += OnTypingStarted;
            App.Discord.MessageCreated += OnMessageCreated;
            App.Discord.MessageDeleted += OnMessageDeleted;
            App.Discord.ChannelUpdated += OnChannelUpdated;
            App.Discord.Resumed += OnResumed;

            TypingUsers = new ObservableCollection<DiscordUser>();
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

        public ObservableCollection<DiscordMessage> Messages
        {
            get => _messages;
            set => OnPropertySet(ref _messages, value);
        }

        public string MessageText
        {
            get => _messageText;
            set => OnPropertySet(ref _messageText, value);
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

        public DiscordUser Recipient => Channel.Type == ChannelType.Private ? (Channel as DiscordDmChannel).Recipients[0] : null;

        public ObservableCollection<DiscordUser> TypingUsers { get; set; }

        public ObservableCollection<FileUploadModel> FileUploads { get; set; }

        public DiscordMessage ReplyTo { get => _replyTo; set => OnPropertySet(ref _replyTo, value); }

        public bool ReplyPing { get => _replyPing; set => OnPropertySet(ref _replyPing, value); }

        public string Topic => Channel.Topic != null ? Channel.Topic.Replace(new[] { "\r\n", "\r", "\n" }, " ").Truncate(512, "...") : string.Empty;

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
                    return Strings.NaturalJoin(dm.Recipients.Select(r => r.Username));
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The suffix of the channel's display name. (i.e. #6402)
        /// </summary>
        public string ChannelSuffix =>
            Channel is DiscordDmChannel dm && dm.Type == ChannelType.Private ? $"#{dm.Recipients[0].Discriminator}" : string.Empty;

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
                    if (dm.Type == ChannelType.Private && dm.Recipients[0] != null)
                        return dm.Recipients[0].AvatarUrl;

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
        public int UploadLimit => App.Discord.CurrentUser.UploadLimit();

        public ulong UploadSize => (ulong)FileUploads.Sum(u => (double)u.Length);

        public string DisplayUploadSize => Tools.ToFileSizeString(UploadSize);

        public string DisplayUploadLimit => Tools.ToFileSizeString(UploadLimit);

        public double UploadProgressBarValue => Math.Min(UploadSize, (ulong)UploadLimit);

        public Brush UploadInfoForeground => !CanSend ? (Brush)Application.Current.Resources["ErrorTextForegroundBrush"] : null;

        public bool ShowUploads => FileUploads.Any() || IsTranscoding;

        public bool ShowUserlistButton => Channel.Type == ChannelType.Group || Channel.Guild != null;

        public bool HasNitro => Channel.Discord.CurrentUser.HasNitro();

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
            => WindowingService.Current.Supported && WindowingService.Current.IsMainWindow(_windowHandle);

        public bool IsPinned =>
            SecondaryTile.Exists($"Channel_{Channel.Id}");

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
                            UnsafeInvokePropertyChanged(nameof(ShowTypingUsers));
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
                // Channel = e.ChannelAfter;
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
            Analytics.TrackEvent("ChannelViewModel_LoadMessages");

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
            _ = t.RequestMissingMembersAsync(messages);

            foreach (var mess in messages)
            {
                //if (!t.Messages.Any(m => m.Id == mess.Id))
                //{
                t.Messages.Insert(index + 1, mess);
                //}
            }
        }, this);

        private void ClearAndAddMessages(IEnumerable<DiscordMessage> messages) => _context.Post(d =>
        {
            var t = d as ChannelViewModel;
            t.Messages.Clear();
            _ = t.RequestMissingMembersAsync(messages);

            Messages.Clear();
            Messages = new ObservableCollection<DiscordMessage>(messages.Reverse());
        }, this);

        private async Task RequestMissingMembersAsync(IEnumerable<DiscordMessage> messages)
        {
            if (Channel.Guild != null)
            {
                var usersToSync = messages.Select(m => m.Author).OfType<DiscordMember>().Where(u => u.IsLocal).Distinct();
                Analytics.TrackEvent("ChannelViewModel_RequestMembers", new Dictionary<string, string> { ["Count"] = $"{usersToSync.Count()}" });

                if (usersToSync.Any())
                    await Channel.Guild.RequestUserPresencesAsync(usersToSync);
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

        public string ProcessMessageText(string input)
        {
            // step 1: normalise whitespace
            var output = input.Replace('\r', '\n');
            var index = 0;

            while ((index = output.IndexOf('@', index)) != -1)
            {
                var index2 = -1;
                if ((index2 = output.IndexOf('#', index)) != -1)
                {
                    // process a user (definitely)

                    var username = output.Substring(index, index2 - index);
                    var discriminator = output.Substring(index2, 4);

                    
                }

                // process a user *or* role
            }

            // TODO: emoji, channels

            return output;
        }

        /// <summary>
        /// Abstracts sending a message.
        /// </summary>
        /// <returns></returns>
        public async Task SendMessageAsync(IProgress<double?> progress = null)
        {
            if (Channel.Type == ChannelType.Voice)
            {
                return;
            }

            Analytics.TrackEvent("ChannelViewModel_SendMessage");

            if ((!string.IsNullOrWhiteSpace(MessageText) || FileUploads.Any()) && CanSend)
            {
                var txt = MessageText ?? "";
                txt = txt.Replace('\r', '\n'); // this is incredibly stupid

                var replyTo = ReplyTo;
                var replyPing = ReplyPing;
                var models = FileUploads.ToArray();

                ReplyTo = null;
                ReplyPing = true;
                MessageText = "";
                FileUploads.Clear();

                var mentions = new List<IMention> { UserMention.All, RoleMention.All, EveryoneMention.All };
                if (replyPing && replyTo != null)
                    mentions.Add(RepliedUserMention.All);

                if (models.Any())
                {
                    var files = new Dictionary<string, IInputStream>();
                    foreach (var item in models)
                    {
                        files.Add(item.Spoiler ? $"SPOILER_{item.FileName}" : item.FileName, await item.GetStreamAsync().ConfigureAwait(false));
                    }

                    await Tools.SendFilesWithProgressAsync(Channel, txt, mentions, replyTo, files, progress).ConfigureAwait(false);

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
                    await Channel.SendMessageAsync(txt, mentions: mentions, replyTo: replyTo).ConfigureAwait(false);
                }


                if (!ImmuneToSlowMode)
                {
                    _messageLastSent = DateTimeOffset.Now;
                    SlowModeTimeout = PerUserRateLimit;
                    InvokePropertyChanged(nameof(CanSend));
                    _context.Post(o => ((DispatcherTimer)o).Start(), _slowModeTimer);
                }
            }
        }

        public void TruncateMessages(int max = 50)
        {
            if (Messages.Count > max)
            {
                Messages = new ObservableCollection<DiscordMessage>(Messages.TakeLast(max));
            }
        }

        #region Typing

        public async Task TriggerTypingAsync()
        {
            Analytics.TrackEvent("ChannelViewModel_TriggerTyping");

            if ((DateTimeOffset.Now - _typingLastSent).Seconds > 10)
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
                    UnsafeInvokePropertyChanged(nameof(ShowTypingUsers));
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
                    UnsafeInvokePropertyChanged(nameof(ShowTypingUsers));
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
