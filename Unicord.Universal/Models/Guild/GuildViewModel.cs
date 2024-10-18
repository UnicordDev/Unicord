using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Commands.Generic;
using Unicord.Universal.Commands.Guild;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.User;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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

            CurrentMember = new UserViewModel(discord.CurrentUser.Id, guildId, this);

            WeakReferenceMessenger.Default.Register<GuildViewModel, ChannelUnreadUpdateEventArgs>(this, (r, m) => r.OnChannelUnreadUpdate(m.Event));
            WeakReferenceMessenger.Default.Register<GuildViewModel, ReadStateUpdateEventArgs>(this, (r, m) => r.OnReadStateUpdated(m.Event));

            AcknowledgeCommand = new AcknowledgeGuildCommand(this);
            ToggleMuteCommand = new MuteGuildCommand(this);
            CopyUrlCommand = new CopyUrlCommand(this);
            CopyIdCommand = new CopyIdCommand(this);
        }

        public ulong Id
            => _guildId;

        public DiscordGuild Guild
            => discord.TryGetCachedGuild(Id, out var guild) ? guild : throw new InvalidOperationException();

        public UserViewModel CurrentMember { get; }

        public string Name
            => Guild.Name;

        public string IconUrl
            => Guild.GetIconUrl(64);

        public ImageSource Icon
            => IconUrl != null ? new BitmapImage(new Uri(IconUrl)) : null;

        public bool Muted
            => Guild.IsMuted();

        public bool Unread
            => !Muted && AccessibleChannels.Any(r => !r.NotificationMuted && r.Unread);

        internal IEnumerable<ChannelViewModel> AccessibleChannels
        {
            get
            {
                if (_accessibleChannels == null)
                    PopulateAccessibleChannels();

                return _accessibleChannels.Values;
            }
        }

        public ICommand AcknowledgeCommand { get; }
        public ICommand ToggleMuteCommand { get; }
        public ICommand EditGuildCommand { get; }
        public ICommand LeaveServerCommand { get; }
        public ICommand CopyUrlCommand { get; }
        public ICommand CopyIdCommand { get; }

        private Task OnReadStateUpdated(ReadStateUpdateEventArgs e)
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

        protected virtual void OnReadStateUpdatedCore(ReadStateUpdateEventArgs e) { }
    }
}
