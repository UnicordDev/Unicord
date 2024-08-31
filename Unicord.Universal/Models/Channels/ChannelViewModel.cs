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
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Commands.Channels;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.User;

namespace Unicord.Universal.Models.Channels
{
    public class ChannelViewModel : ViewModelBase, IEquatable<ChannelViewModel>, IEquatable<DiscordChannel>, ISnowflake
    {
        private readonly ulong _channelId;
        private UserViewModel _recipient;
        private ReadStateViewModel _readStateCache;

        internal ChannelViewModel(ulong channelId, bool isTransient = false, ViewModelBase parent = null)
            : base(parent)
        {
            this._channelId = channelId;

            // BUGBUG: PERF, this doesn't appreciate being used objects with short lifetimes
            if (!isTransient)
            {
                WeakReferenceMessenger.Default.Register<ChannelViewModel, ChannelUpdateEventArgs>(this, (r, m) => r.OnChannelUpdated(m.Event));
                WeakReferenceMessenger.Default.Register<ChannelViewModel, ReadStateUpdatedEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));
            }
        }

        public ulong Id
            => _channelId;

        public virtual DiscordChannel Channel 
            => discord.InternalGetCachedChannel(Id);

        public virtual ChannelViewModel Parent => Channel.ParentId != null ?
            new ChannelViewModel(Channel.ParentId.Value, false, this) :
            null;

        public virtual GuildViewModel Guild => Channel.GuildId != 0 ?
            new GuildViewModel(Channel.GuildId, this) :
            null;

        public virtual string Name
            => Channel.Name;
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
            => _recipient ??= (ChannelType == ChannelType.Private && Channel is DiscordDmChannel DM ? new UserViewModel(DM.Recipient, null, null) : null);
        public bool Muted
            => Channel.IsMuted();
        public int? NullableMentionCount 
            => ReadState.MentionCount == 0 ? null : ReadState.MentionCount;

        private void OnReadStateUpdated(ReadStateUpdatedEventArgs e)
        {
            if (e.ReadState.Id != Channel.Id)
                return;

            InvokePropertyChanged(nameof(Unread));
            InvokePropertyChanged(nameof(NullableMentionCount));
        }

        protected virtual Task OnChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (e.ChannelAfter.Id != Id)
                return Task.CompletedTask;

            if (e.ChannelAfter.Name != e.ChannelBefore.Name)
                InvokePropertyChanged(nameof(Name));
            if (e.ChannelAfter.Topic != e.ChannelBefore.Topic)
                InvokePropertyChanged(nameof(Topic));
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
