using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Channel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Windows.System;

namespace Unicord.Universal.Models.Guild
{
    public class GuildViewModel : ViewModelBase, ISnowflake
    {
        private readonly ulong _guildId;
        private Dictionary<ulong, ChannelViewModel> _accessibleChannels;

        public GuildViewModel(ulong guildId, ViewModelBase parent = null)
            : base(parent)
        {
            _guildId = guildId;

            WeakReferenceMessenger.Default.Register<GuildViewModel, ChannelUnreadUpdateEventArgs>(this, (r, m) => r.OnChannelUnreadUpdate(m.Event));
            WeakReferenceMessenger.Default.Register<GuildViewModel, ReadStateUpdatedEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));
        }

        public ulong Id
            => _guildId;

        public DiscordGuild Guild
            => discord.InternalGetCachedGuild(Id);

        public string Name =>
            Guild.Name;

        public string IconUrl =>
            Guild.GetIconUrl(64);

        public bool Muted
            => Guild.IsMuted();

        public bool Unread =>
            !Muted && AccessibleChannels.Any(r => !r.NotificationMuted && r.Unread);

        internal IEnumerable<ChannelViewModel> AccessibleChannels
        {
            get
            {
                if (_accessibleChannels == null)
                    PopulateAccessibleChannels();

                return _accessibleChannels.Values;
            }
        }

        private Task OnReadStateUpdated(ReadStateUpdatedEventArgs e)
        {
            if (Guild.Channels.ContainsKey(e.ReadState.Id))
            {
                InvokePropertyChanged(nameof(Unread));
                OnReadStateUpdatedCore(e);
            }

            return Task.CompletedTask;
        }

        private Task OnChannelUnreadUpdate(ChannelUnreadUpdateEventArgs e)
        {
            if (e.GuildId == Id)
            {
                InvokePropertyChanged(nameof(Unread));
            }

            return Task.CompletedTask;
        }

        // TODO: update this cache when the user's roles change, and when the guild gets updated, and when a new channel gets created :D
        private void PopulateAccessibleChannels()
        {
            _accessibleChannels = new Dictionary<ulong, ChannelViewModel>();

            if ((Guild.CurrentMember?.IsOwner ?? true))
            {
                foreach (var (key, value) in Guild.Channels)
                {
                    _accessibleChannels[key] = new ChannelViewModel(value.Id, true, this);
                }
            }
            else
            {
                foreach (var (key, value) in Guild.Channels)
                {
                    if (!value.PermissionsFor(Guild.CurrentMember).HasFlag(Permissions.AccessChannels))
                        continue;

                    if (value.Parent != null && !value.Parent.PermissionsFor(Guild.CurrentMember).HasFlag(Permissions.AccessChannels))
                        continue;

                    _accessibleChannels[key] = new ChannelViewModel(value.Id, true, this);
                }
            }
        }

        protected virtual void OnReadStateUpdatedCore(ReadStateUpdatedEventArgs e) { }
    }
}
