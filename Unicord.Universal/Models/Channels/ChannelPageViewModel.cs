using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Windows;
using Humanizer;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using Unicord.Universal.Commands.Channels;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Models.Messaging;
using Unicord.Universal.Models.User;
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
    public class ChannelPageViewModel : ChannelPageViewModelBase, IDisposable
    {
        private const int INITIAL_LOAD_LIMIT = 50;

        private readonly DiscordChannel _channel;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _typingCancellation;
        private readonly WindowHandle _windowHandle;
        private readonly ResourceLoader _strings;
        private readonly SemaphoreSlim _loadSemaphore;
        private readonly DispatcherTimer _slowModeTimer;

        private DiscordUser _currentUser;
        private double _slowModeTimeout;
        private DateTimeOffset _typingLastSent;
        private DateTimeOffset _messageLastSent;
        private string _messageText;
        private bool _isTranscoding;
        private ObservableCollection<MessageViewModel> _messages;
        private MessageViewModel _replyTo;
        private bool _replyPing = true;

        public ChannelPageViewModel(DiscordChannel channel,
                                    WindowHandle window,
                                    ICommand enterEditMode = null,
                                    ICommand exitEditMode = null)
            : base(channel)
        {
            if (!channel.IsText())
                throw new InvalidOperationException("Unable to create a view model for a non-text channel"); // no op this

            _windowHandle = window;
            _strings = ResourceLoader.GetForCurrentView("ChannelPage");
            _loadSemaphore = new SemaphoreSlim(1, 1);
            _typingCancellation = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _typingLastSent = DateTime.Now - TimeSpan.FromSeconds(10);

            _channel = channel;
            _currentUser = channel.Guild?.CurrentMember ?? App.Discord.CurrentUser;
            _messages = new ObservableCollection<MessageViewModel>();

            WeakReferenceMessenger.Default.Register<ChannelPageViewModel, TypingStartEventArgs>(this, (t, v) => t.OnTypingStarted(v.Event));
            WeakReferenceMessenger.Default.Register<ChannelPageViewModel, MessageCreateEventArgs>(this, (t, v) => t.OnMessageCreated(v.Event));
            WeakReferenceMessenger.Default.Register<ChannelPageViewModel, MessageDeleteEventArgs>(this, (t, v) => t.OnMessageDeleted(v.Event));
            WeakReferenceMessenger.Default.Register<ChannelPageViewModel, ResumedEventArgs>(this, (t, v) => t.OnResumed(v.Event));

            TypingUsers = new ObservableCollection<UserViewModel>();
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

            _slowModeTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1 / 60.0) };
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

            EnterEditModeCommand = enterEditMode;
            ExitEditModeCommand = exitEditMode;
            ClearReplyCommand = new RelayCommand(() => this.ReplyTo = null);
            MassDeleteCommand = new AsyncRelayCommand(async () =>
            {
                var loader = ResourceLoader.GetForCurrentView("ChannelPage");
                if (await UIUtilities.ShowYesNoDialogAsync(loader.GetString("MassDeleteTitle"), loader.GetString("MassDeleteMessage"), "\xE74D"))
                {
                    Analytics.TrackEvent("ChannelPage_MassDeleteMessage");
                    var items = Messages.Where(m => m.IsSelected)
                                        .ToArray();

                    ExitEditModeCommand?.Execute(null);

                    foreach (var item in items)
                    {
                        await item.Message.DeleteAsync();
                        await Task.Delay(500);
                    }
                }
            });
        }

        public ObservableCollection<MessageViewModel> Messages
        {
            get => _messages;
            set => OnPropertySet(ref _messages, value);
        }

        public string MessageText
        {
            get => _messageText;
            set => OnPropertySet(ref _messageText, value);
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

        public ObservableCollection<UserViewModel> TypingUsers { get; set; }

        public ObservableCollection<FileUploadModel> FileUploads { get; set; }

        public MessageViewModel ReplyTo
        {
            get => _replyTo; set
            {
                OnPropertySet(ref _replyTo, value);
                InvokePropertyChanged(nameof(ShowReply));
            }
        }

        public bool ReplyPing { get => _replyPing; set => OnPropertySet(ref _replyPing, value); }

        public bool ShowReply =>
            ReplyTo != null;

        public override string Topic
            => Channel.Topic != null ? Channel.Topic.Replace(new[] { "\r\n", "\r", "\n" }, " ").Truncate(512, "...") : string.Empty;

        /// <summary>
        /// The channel's symbol. (i.e. #, @ etc.)
        /// </summary>
        public string ChannelPrefix =>
            Channel.Guild != null && !Channel.IsThread ? "#"
            : Channel.Type == ChannelType.Private ? "@"
            : string.Empty;


        /// <summary>
        /// The suffix of the channel's display name. (i.e. #6402)
        /// </summary>
        public string ChannelSuffix =>
            string.Empty;

        /// <summary>
        /// The full channel name (i.e. @WamWooWam#6402, #general, etc.)
        /// </summary>
        public string FullChannelName =>
            $"{ChannelPrefix}{DisplayName}{ChannelSuffix}";

        /// <summary>
        /// The current placeholder to display in the message text box
        /// </summary>
        public string ChannelPlaceholder =>
           CanSend || (SlowModeTimeout != 0 && !ImmuneToSlowMode) ?
            string.Format(_strings.GetString("MessageChannelFormat"), ChannelPrefix, DisplayName) :
            _strings.GetString("ChannelReadonlyText");

        public bool CanType => CanSend || (SlowModeTimeout != 0 && !ImmuneToSlowMode);

        /// <summary>
        /// The title for the page displaying the channel
        /// </summary>
        public string TitleText => Channel.Guild != null ? $"{FullChannelName} - {Channel.Guild.Name}" : FullChannelName;


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

        // TODO: "ChannelPageUploadViewModel"
        public int UploadLimit => App.Discord.CurrentUser.UploadLimit();

        public ulong UploadSize => (ulong)FileUploads.Sum(u => (double)u.Length);

        public string DisplayUploadSize => Tools.ToFileSizeString(UploadSize);

        public string DisplayUploadLimit => Tools.ToFileSizeString(UploadLimit);

        public double UploadProgressBarValue => Math.Min(UploadSize, (ulong)UploadLimit);

        // TODO: refactor this
        public Brush UploadInfoForeground => !CanSend ? (Brush)Application.Current.Resources["ErrorTextForegroundBrush"] : null;

        public bool ShowUploads => FileUploads.Any() || IsTranscoding;

        public bool HasNitro => App.Discord.CurrentUser.HasNitro();

        public bool ShowEditButton
            => Channel.Guild != null && Permissions.HasPermission(Permissions.ManageChannels);

        public bool ShowSlowMode
            => Channel.PerUserRateLimit.HasValue && Channel.PerUserRateLimit != 0;

        public string SlowModeText
            => string.Format(_strings.GetString(ImmuneToSlowMode ? "ImmuneSlowModeFormat" : "SlowModeFormat"), TimeSpan.FromSeconds(Channel.PerUserRateLimit ?? 0).ToNaturalString());

        private bool ImmuneToSlowMode
            => Permissions.HasPermission(Permissions.ManageMessages) && Permissions.HasPermission(Permissions.ManageChannels);

        public bool ShowTypingUsers
            => TypingUsers?.Any() == true;

        public bool ShowTypingContainer =>
            ShowSlowMode || ShowTypingUsers;
        public bool HideTypingContainer =>
            !ShowTypingContainer;

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

        public ICommand EnterEditModeCommand { get; }
        public ICommand ExitEditModeCommand { get; }
        public ICommand MassDeleteCommand { get; }
        public ICommand ClearReplyCommand { get; }

        public bool IsDisposed { get; internal set; }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Channel != Channel) return;

            await _loadSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_typingCancellation.TryGetValue(e.Author.Id, out var src))
                {
                    src.Cancel();
                }

                var usr = TypingUsers.FirstOrDefault(u => u.Id == e.Author.Id);
                if (usr != null)
                {
                    syncContext.Post(a =>
                    {
                        TypingUsers.Remove(usr);
                        InvokePropertyChanged(nameof(ShowTypingUsers));
                        InvokePropertyChanged(nameof(ShowTypingContainer));
                        InvokePropertyChanged(nameof(HideTypingContainer));
                    }, null);
                }

                if (string.IsNullOrWhiteSpace(e.Message.Author.Username))
                    _ = RequestMissingMembersAsync(new[] { e.Message });

                if (!Messages.Any(m => m.Id == e.Message.Id))
                {
                    syncContext.Post(a => AddMessage(e.Message), null);
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }

        }

        private Task OnMessageDeleted(MessageDeleteEventArgs e)
        {
            if (e.Channel != Channel) return Task.CompletedTask;

            // BUGBUG: These are fire and forget, may be better to refactor into an async event setup?
            syncContext.Post(a => RemoveMessage(e.Message), null);
            return Task.CompletedTask;
        }

        protected override async Task OnChannelUpdated(ChannelUpdateEventArgs e)
        {
            await base.OnChannelUpdated(e);

            // todo: what changed
            if (e.ChannelAfter.Id == _channel.Id)
            {
                // Channel = e.ChannelAfter;
            }
        }

        private async Task OnResumed(ResumedEventArgs e)
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

            if (Channel.Guild != null)
            {
                App.RoamingSettings.Save($"GuildPreviousChannels::{Channel.Guild.Id}", Channel.Id);
            }

            App.LocalSettings.Save("LastViewedChannel", Channel.Id);

            try
            {
                IsLoading = true;

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
                IsLoading = false;
                _loadSemaphore.Release();
            }
        }

        private void InsertMessages(int index, IEnumerable<DiscordMessage> messages) => syncContext.Post(d =>
        {
            _ = RequestMissingMembersAsync(messages);

            foreach (var mess in messages)
            {
                InsertMessage(index + 1, mess);
            }
        }, this);

        private void ClearAndAddMessages(IEnumerable<DiscordMessage> messages) => syncContext.Post(d =>
        {
            _ = RequestMissingMembersAsync(messages);

            Messages.Clear();
            foreach (var mess in messages.Reverse())
            {
                AddMessage(mess);
            }

        }, null);

        private async Task RequestMissingMembersAsync(IEnumerable<DiscordMessage> messages)
        {
            if (Channel.Guild != null)
            {
                var mentionedUsers = messages.SelectMany(m => m.MentionedUsers);
                var usersToSync = messages.Select(m => m.Author)
                                          .Where(u => string.IsNullOrWhiteSpace(u.Username) || (u is not DiscordMember m || m.IsLocal))
                                          .Concat(mentionedUsers)
                                          .Select(u => u.Id)
                                          .Distinct();

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
                    var messages = await Channel.GetMessagesBeforeAsync(message.Id, INITIAL_LOAD_LIMIT)
                        .ConfigureAwait(false);
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
        public async Task SendMessageAsync()
        {
            if (Channel.Type == ChannelType.Voice)
            {
                return;
            }
            try
            {
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
                        IsUploading = true;
                        var files = new Dictionary<string, IInputStream>();
                        foreach (var item in models)
                        {
                            files.Add(item.Spoiler ? $"SPOILER_{item.FileName}" : item.FileName, await item.GetStreamAsync().ConfigureAwait(false));
                        }

                        var progress = new Progress<double?>((p) =>
                        {
                            if (p == null && !IsUploadIndeterminate)
                            {
                                IsUploadIndeterminate = true;
                            }
                            else
                            {
                                IsUploadIndeterminate = false;
                                UploadProgress = p.Value;
                            }
                        });

                        await Channel.SendFilesWithProgressAsync(Tools.HttpClient, txt, mentions, replyTo?.Message, files, progress)
                                   .ConfigureAwait(false);

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
                        await Channel.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent(txt)
                            .WithReply(replyTo?.Id)
                            .WithAllowedMentions(mentions)).ConfigureAwait(false);
                    }


                    if (!ImmuneToSlowMode)
                    {
                        _messageLastSent = DateTimeOffset.Now;
                        SlowModeTimeout = PerUserRateLimit;
                        InvokePropertyChanged(nameof(CanSend));
                        syncContext.Post(o => ((DispatcherTimer)o).Start(), _slowModeTimer);
                    }
                }
            }
            finally
            {
                IsUploading = false;
            }
        }

        public void TruncateMessages(int max = 50)
        {
            if (Messages.Count > max)
            {
                Messages = new ObservableCollection<MessageViewModel>(Messages.TakeLast(max));
            }
        }

        #region Typing

        public async Task TriggerTypingAsync()
        {
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

                syncContext.Post(o =>
                {
                    TypingUsers.Add(new UserViewModel(e.User, Channel.GuildId, this));
                    InvokePropertyChanged(nameof(ShowTypingUsers));
                    InvokePropertyChanged(nameof(ShowTypingContainer));
                    InvokePropertyChanged(nameof(HideTypingContainer));
                }, null);

                HandleTypingStart(e);
            }

            return Task.CompletedTask;
        }

        private void HandleTypingStart(TypingStartEventArgs e)
        {
            var source = new CancellationTokenSource(10_000);
            source.Token.Register(() => syncContext.Post(o =>
            {
                foreach (var user in TypingUsers.Where(u => u.Id == e.User.Id).ToArray())
                    TypingUsers.Remove(user);
                InvokePropertyChanged(nameof(ShowTypingUsers));
                InvokePropertyChanged(nameof(ShowTypingContainer));
                InvokePropertyChanged(nameof(HideTypingContainer));
            }, null));

            _typingCancellation[e.User.Id] = source;
        }

        #endregion

        private void AddMessage(DiscordMessage message)
        {
            Messages.Add(new MessageViewModel(message, this));
        }

        private void InsertMessage(int index, DiscordMessage message)
        {
            Messages.Insert(index, new MessageViewModel(message, this));
        }

        private void RemoveMessage(DiscordMessage message)
        {
            var vm = Messages.FirstOrDefault(m => m.Id == message.Id);
            if (vm == null)
                return;

            Messages.Remove(vm);
        }

        public virtual void Dispose()
        {
            IsDisposed = true;

            Messages.Clear();

            foreach (var item in FileUploads)
            {
                item.Dispose();
            }
        }
    }
}
