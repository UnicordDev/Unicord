using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using CommunityToolkit.Mvvm.Messaging;
using Unicord.Universal.Commands;
using Unicord.Universal.Commands.Channels;
using Unicord.Universal.Commands.Generic;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.User;
using Windows.UI.StartScreen;
using Windows.Networking.Sockets;

namespace Unicord.Universal.Models.Channels
{
    public class ChannelViewModel : ViewModelBase, IEquatable<ChannelViewModel>, IEquatable<DiscordChannel>, ISnowflake
    {
        private readonly ulong _channelId;
        private readonly DiscordChannel _channelCache;

        private UserViewModel _recipientCache;
        private ReadStateViewModel _readStateCache;

        private bool isLoading;
        private bool isUploading;
        private bool isUploadIndeterminate;
        private double uploadProgress;

        internal ChannelViewModel(ulong channelId, bool isTransient = false, ViewModelBase parent = null)
            : base(parent)
        {
            this._channelId = channelId;

            // BUGBUG: PERF, this doesn't appreciate being used by objects with short lifetimes
            if (!isTransient)
            {
                AcknowledgeCommand = new AcknowledgeChannelCommand(this);
                EditCommand = new EditChannelCommand();
                ToggleMuteCommand = new MuteChannelCommand(this);
                PinToStartCommand = new PinChannelToStartCommand(this);
                CopyIdCommand = new CopyIdCommand(this);
                CopyUrlCommand = new CopyUrlCommand(this);
                OpenInNewWindowCommand = new OpenInNewWindowCommand(this, false);
                OpenInCompactOverlayWindowCommand = new OpenInNewWindowCommand(this, true);

                WeakReferenceMessenger.Default.Register<ChannelViewModel, ChannelUpdateEventArgs>(this, static (r, m) => r.OnChannelUpdated(m.Event));
                WeakReferenceMessenger.Default.Register<ChannelViewModel, ReadStateUpdateEventArgs>(this, static (r, m) => r.OnReadStateUpdated(m.Event));
            }
        }

        //
        // So as a rule, we're trying to stick to the cache for channels because we want a single
        // "source of truth" for everything on screen, however sometimes we have to fetch entities
        // via REST, which may not be in the cache, so this constructor accomodates that, but it should
        // be used sparingly.
        //
        internal ChannelViewModel(DiscordChannel channel, bool isTransient = false, ViewModelBase parent = null)
            : this(channel.Id, isTransient, parent)
        {
            this._channelCache = channel;
        }

        public ulong Id
            => _channelId;

        public virtual DiscordChannel Channel
            => (discord.TryGetCachedChannel(Id, out var channel) ? channel :
                 discord.TryGetCachedThread(Id, out var thread) ? thread : _channelCache)
            ?? throw new InvalidOperationException("Unable to find this channel, probably a thread that you've not joined.");

        public virtual ChannelViewModel Parent => Channel.ParentId != null ?
            new ChannelViewModel(Channel.ParentId.Value, false, this) :
            null;

        public virtual GuildViewModel Guild => Channel.GuildId != null && Channel.GuildId != 0 ?
            new GuildViewModel(Channel.GuildId.Value, this) :
            null;

        public virtual string Name
        {
            get
            {
                if (Channel is DiscordDmChannel dm)
                {
                    if (dm.Type == ChannelType.Private && dm.Recipients.Count == 1 && dm.Recipients[0] != null)
                    {
                        return dm.Recipients[0].DisplayName;
                    }

                    if (dm.Type == ChannelType.Group)
                    {
                        return dm.Name ?? dm.Recipients.Select(r => r.DisplayName).Humanize();
                    }
                }

                return Channel.Name;
            }
        }
        public virtual ChannelType ChannelType
            => Channel.Type;
        public virtual int Position
            => Channel.Position;
        public virtual string Topic
            => Channel.Topic;
        public virtual ReadStateViewModel ReadState
            => _readStateCache ??= new ReadStateViewModel(Id, this);
        public virtual bool Unread
            => !Muted && ReadState.Unread;
        public virtual bool NotificationMuted
            => (Muted || (Parent?.Muted ?? false) || (Guild?.Muted ?? false));
        public UserViewModel Recipient
        {
            get
            {
                if (Channel is not DiscordDmChannel dm || dm.Type != ChannelType.Private || dm.Recipients.Count == 0)
                    return null;

                return _recipientCache ??= new UserViewModel(dm.Recipients[0], null, this);
            }
        }

        public bool Muted
            => Channel.IsMuted();
        public int? NullableMentionCount
            => ReadState.MentionCount == 0 ? -1 : ReadState.MentionCount;
        public double MutedOpacity
            => Muted ? 0.5 : 1.0;
        public bool HasTopic
            => !string.IsNullOrWhiteSpace(Topic);

        public bool IsDM
            => Channel.Type is ChannelType.Private;

        public bool IsNotDM
            => !IsDM;

        public Uri IconUrl
        {
            get
            {
                if (Channel is not DiscordDmChannel dm)
                    return null;

                if (dm.Type == ChannelType.Private && dm.Recipients.Count == 1 && dm.Recipients[0] != null)
                {
                    return new Uri(dm.Recipients[0].GetAvatarUrl(64));
                }

                if (dm.Type == ChannelType.Group)
                {
                    if (dm.IconUrl != null) return new Uri(dm.IconUrl + "?size=64");
                    // TODO: default icons?
                }

                return null;
            }
        }

        /// <summary>
        /// The actual channel name. (i.e. general, WamWooWam, etc.)
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Channel.Name))
                {
                    return Channel.Name;
                }

                if (Channel is DiscordDmChannel dm)
                {
                    return dm.Recipients.Select(r => r.DisplayName).Humanize();
                }

                return string.Empty;
            }
        }

        public bool ShouldShowNotificaitonIndicator
        {
            get
            {
                if (Channel is DiscordDmChannel dm)
                {
                    return NullableMentionCount != -1;
                }

                return Unread;
            }
        }

        /// <summary>
        /// The icon to show in the top left of a channel
        /// </summary>
        public string ChannelIconUrl
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

        public bool ShowUserlistButton
            => Channel.Type == ChannelType.Group || Channel.Guild != null;

        public bool IsPinned =>
            SecondaryTile.Exists($"Channel_{Channel.Id}");

        public bool IsLoading { get => isLoading; set => OnPropertySet(ref isLoading, value); }
        public bool IsUploading { get => isUploading; set => OnPropertySet(ref isUploading, value); }
        public bool IsUploadIndeterminate { get => isUploadIndeterminate; set => OnPropertySet(ref isUploadIndeterminate, value); }
        public double UploadProgress { get => uploadProgress; set => OnPropertySet(ref uploadProgress, value); }

        public bool ShowPinsButton
            => Channel.IsText();

        public bool ShowExtendedItems
            => Channel.IsText();

        public ICommand AcknowledgeCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ToggleMuteCommand { get; }
        public ICommand PinToStartCommand { get; }
        public ICommand CopyIdCommand { get; }
        public ICommand CopyUrlCommand { get; }
        public ICommand OpenInNewWindowCommand { get; }
        public ICommand OpenInCompactOverlayWindowCommand { get; }

        private void OnReadStateUpdated(ReadStateUpdateEventArgs e)
        {
            if (e.ReadState.Id != Channel.Id)
                return;

            InvokePropertyChanged(nameof(Unread));
            InvokePropertyChanged(nameof(ShouldShowNotificaitonIndicator));
            InvokePropertyChanged(nameof(NullableMentionCount));
        }

        protected virtual Task OnChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (e.ChannelAfter.Id != Id)
                return Task.CompletedTask;

            if (e.ChannelAfter.Name != e.ChannelBefore.Name)
                InvokePropertyChanged(nameof(Name));

            if (e.ChannelAfter.Topic != e.ChannelBefore.Topic)
            {
                InvokePropertyChanged(nameof(Topic));
                InvokePropertyChanged(nameof(HasTopic));
            }

            if (e.ChannelAfter.Position != e.ChannelBefore.Position)
                InvokePropertyChanged(nameof(Position));
            if (e.ChannelAfter.Type != e.ChannelBefore.Type)
                InvokePropertyChanged(nameof(ChannelType));

            return Task.CompletedTask;
        }

        public bool Equals(ChannelViewModel other)
        {
            return other.Channel.Id == Channel.Id;
        }

        public bool Equals(DiscordChannel other)
        {
            return other.Id == Channel.Id;
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                ChannelViewModel vm => Equals(vm),
                DiscordChannel ch => Equals(ch),
                _ => base.Equals(obj),
            };
        }

        public override int GetHashCode()
        {
            return 329305889 + EqualityComparer<ulong>.Default.GetHashCode(Id);
        }
    }
}
