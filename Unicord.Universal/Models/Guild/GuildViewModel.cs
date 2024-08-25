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
        private readonly DiscordMember _currentMember = null;
        private ConcurrentDictionary<ulong, ChannelViewModel> _accessibleChannels;

        public GuildViewModel(DiscordGuild guild, ViewModelBase parent = null)
            : base(parent)
        {
            Guild = guild;
            _currentMember = guild.CurrentMember;
            _accessibleChannels = new ConcurrentDictionary<ulong, ChannelViewModel>();

            PopulateAccessibleChannels();

            WeakReferenceMessenger.Default.Register<GuildViewModel, ReadStateUpdatedEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));
        }

        public ulong Id
            => Guild.Id;

        public DiscordGuild Guild { get; set; }

        public string Name =>
            Guild.Name;

        public string IconUrl =>
            Guild.GetIconUrl(64);

        public bool Muted
            => Guild.IsMuted();

        public bool Unread =>
            !Muted && AccessibleChannels.Any(r => !r.NotificationMuted && r.Unread);

        internal IEnumerable<ChannelViewModel> AccessibleChannels
            => _accessibleChannels.Values;

        private Task OnReadStateUpdated(ReadStateUpdatedEventArgs e)
        {
            if (_accessibleChannels.ContainsKey(e.ReadState.Id))
            {
                InvokePropertyChanged(nameof(Unread));
                OnReadStateUpdatedCore(e);
            }

            return Task.CompletedTask;
        }

        // TODO: update this cache when the user's roles change, and when the guild gets updated, and when a new channel gets created :D
        private void PopulateAccessibleChannels()
        {
            _accessibleChannels.Clear();

            if ((_currentMember?.IsOwner ?? true))
            {
                foreach (var (key, value) in Guild.Channels)
                {
                    _accessibleChannels[key] = new ChannelViewModel(value, true, this);
                }
            }
            else
            {
                foreach (var (key, value) in Guild.Channels)
                {
                    if (!value.PermissionsFor(_currentMember).HasFlag(Permissions.AccessChannels)) 
                        continue;

                    if (value.Parent != null && !value.Parent.PermissionsFor(_currentMember).HasFlag(Permissions.AccessChannels))
                        continue;

                    _accessibleChannels[key] = new ChannelViewModel(value, true, this);
                }
            }
        }

        protected virtual void OnReadStateUpdatedCore(ReadStateUpdatedEventArgs e) { }
    }
}
