using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Guild;
using Unicord.Universal.Models.User;

namespace Unicord.Universal.Models.Channels
{
    public class ChannelViewModel : ViewModelBase, IEquatable<ChannelViewModel>, IEquatable<DiscordChannel>
    {
        private readonly DiscordChannel _channel;
        private UserViewModel _recipient;
        private ReadStateViewModel _readStateCache;

        internal ChannelViewModel(DiscordChannel channel, bool isTransient = false, ViewModelBase parent = null)
            : base(parent)
        {
            this._channel = channel;

            // BUGBUG: PERF, this doesn't appreciate being used objects with short lifetimes
            if (!isTransient)
            {
                WeakReferenceMessenger.Default.Register<ChannelViewModel, ChannelUpdateEventArgs>(this, (r, m) => r.OnChannelUpdated(m.Event));
                WeakReferenceMessenger.Default.Register<ChannelViewModel, ReadStateUpdatedEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));
            }
        }

        public ulong Id
            => Channel.Id;

        public virtual DiscordChannel Channel => _channel;

        public virtual ChannelViewModel Parent => Channel.Parent != null ?
            new ChannelViewModel(Channel.Parent, false, this) :
            null;

        public virtual GuildViewModel Guild => Channel.Guild != null ?
            new GuildViewModel(Channel.Guild, this) :
            null;

        public virtual string Name
            => _channel.Name;
        public virtual ChannelType ChannelType
            => _channel.Type;
        public virtual int Position
            => _channel.Position;
        public virtual string Topic
            => _channel.Topic;
        public virtual ReadStateViewModel ReadState
            => _readStateCache ??= new ReadStateViewModel(Channel, this);
        public virtual bool Unread
            => !Muted && ReadState.Unread;
        public virtual bool NotificationMuted
            => (Muted || (Parent?.Muted ?? false) || (Guild?.Muted ?? false));

        public UserViewModel Recipient
            => _recipient ??= (ChannelType == ChannelType.Private && Channel is DiscordDmChannel DM ? new UserViewModel(DM.Recipient, null) : null);

        public bool Muted
            => Channel.IsMuted();

        public int? NullableMentionCount => ReadState.MentionCount == 0 ? null : ReadState.MentionCount;

        private void OnReadStateUpdated(ReadStateUpdatedEventArgs e)
        {
            if (e.ReadState.Id != Channel.Id)
                return;

            InvokePropertyChanged(nameof(Unread));
            InvokePropertyChanged(nameof(NullableMentionCount));
        }

        protected virtual Task OnChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (e.ChannelAfter.Id != _channel.Id)
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
            return 329305889 + EqualityComparer<DiscordChannel>.Default.GetHashCode(_channel);
        }
    }
}
