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
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Commands;
using Unicord.Universal.Commands.Channels;
using Unicord.Universal.Commands.Generic;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.User;

namespace Unicord.Universal.Models.Channels
{
    public class ChannelViewModel : ViewModelBase, IEquatable<ChannelViewModel>, IEquatable<DiscordChannel>, ISnowflake
    {
        private readonly ulong _channelId;
        private UserViewModel _recipientCache;
        private ReadStateViewModel _readStateCache;

        internal ChannelViewModel(ulong channelId, bool isTransient = false, ViewModelBase parent = null)
            : base(parent)
        {
            this._channelId = channelId;

            // BUGBUG: PERF, this doesn't appreciate being used objects with short lifetimes
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

                WeakReferenceMessenger.Default.Register<ChannelViewModel, ChannelUpdateEventArgs>(this, (r, m) => r.OnChannelUpdated(m.Event));
                WeakReferenceMessenger.Default.Register<ChannelViewModel, ReadStateUpdateEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));
            }
        }

        public ulong Id
            => _channelId;

        public virtual DiscordChannel Channel
            => discord.TryGetCachedChannel(Id, out var channel) ? channel :
               discord.TryGetCachedThread(Id, out var thread) ? thread :
            throw new InvalidOperationException();

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
                if (Channel is not DiscordDmChannel dm || dm.Type != ChannelType.Private) 
                    return null;

                return _recipientCache ??= new UserViewModel(dm.Recipients[0], null, this);
            }
        }

        public bool Muted
            => Channel.IsMuted();
        public int? NullableMentionCount
            => ReadState.MentionCount == 0 ? null : ReadState.MentionCount;

        public double MutedOpacity
            => Muted ? 0.5 : 1.0;
        public bool HasTopic
            => !string.IsNullOrWhiteSpace(Topic);

        public string IconUrl
        {
            get
            {
                if (Channel is not DiscordDmChannel dm)
                    return "";

                if (dm.Type == ChannelType.Private && dm.Recipients.Count == 1 && dm.Recipients[0] != null)
                {
                    return dm.Recipients[0].GetAvatarUrl(64);
                }

                if (dm.Type == ChannelType.Group)
                {
                    if (dm.IconUrl != null) return dm.IconUrl + "?size=64";
                    // TODO: default icons?
                }

                return "";
            }
        }

        public bool ShouldShowNotificaitonIndicator
        {
            get
            {
                if (Channel is DiscordDmChannel dm)
                {
                    return NullableMentionCount != null;
                }

                return Unread;
            }
        }

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
