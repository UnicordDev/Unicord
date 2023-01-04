using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models
{
    internal interface IGuildListViewModel
    {
        string Name { get; }
        bool Unread { get; }
        int MentionCount { get; }
        bool TryGetModelForGuild(DiscordGuild guild, out GuildListViewModel model);
    }

    internal class GuildListFolderViewModel : ViewModelBase, IGuildListViewModel
    {
        public DiscordGuildFolder _folder;
        private bool _isExpanded;

        public GuildListFolderViewModel(DiscordGuildFolder folder, IEnumerable<DiscordGuild> guilds)
        {
            _folder = folder;
            Children = new ObservableCollection<GuildListViewModel>();

            foreach (var guild in guilds)
                Children.Add(new GuildListViewModel(guild, this));
        }

        public string Name => _folder.Name;

        public DiscordColor Color => _folder.Color ?? default;

        public bool Unread => Children.Any(g => g.Unread);

        public int MentionCount
        {
            get
            {
                var v = Children.Sum(r => r.AccessibleChannels.Sum(r => r.ReadState.MentionCount));
                return v == 0 ? -1 : v;
            }
        }

        public bool IsExpanded { get => _isExpanded; set => OnPropertySet(ref _isExpanded, value); }

        public ObservableCollection<GuildListViewModel> Children { get; set; }

        public string Icon1 => Children.ElementAtOrDefault(0)?.IconUrl;
        public string Icon2 => Children.ElementAtOrDefault(1)?.IconUrl;
        public string Icon3 => Children.ElementAtOrDefault(2)?.IconUrl;
        public string Icon4 => Children.ElementAtOrDefault(3)?.IconUrl;

        public bool TryGetModelForGuild(DiscordGuild guild, out GuildListViewModel model)
        {
            foreach (var child in Children)
            {
                if (child.TryGetModelForGuild(guild, out model))
                    return true;
            }

            model = null;
            return false;
        }
    }

    internal class GuildListViewModel : ViewModelBase, IGuildListViewModel
    {
        private DiscordMember _currentMember;
        private GuildListFolderViewModel _parent;
        private bool _isSelected;

        public GuildListViewModel(DiscordGuild guild, GuildListFolderViewModel parent = null)
        {
            Guild = guild;

            _parent = parent;
            _currentMember = guild.CurrentMember;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvokePropertyChanged(e.PropertyName);
        }

        public bool TryGetModelForGuild(DiscordGuild guild, out GuildListViewModel model)
        {
            if (Guild == guild)
            {
                model = this;
                return true;
            }

            model = null;
            return false;
        }

        public DiscordGuild Guild { get; set; }

        public string Name =>
            Guild.Name;

        public string IconUrl =>
            Guild.IconUrl;

        public bool Muted =>
            Guild.Muted;

        public bool Unread =>
            !Guild.Muted && AccessibleChannels.Any(r => !r.NotificationMuted && r.ReadState.Unread);

        public bool IsSelected
        {
            get => _isSelected;
            set => OnPropertySet(ref _isSelected, value);
        }

        public int MentionCount
        {
            get
            {
                var v = AccessibleChannels.Sum(r => r.ReadState.MentionCount);
                return (Guild.Muted || v == 0) ? -1 : v;
            }
        }

        // todo: cache.
        internal IEnumerable<DiscordChannel> AccessibleChannels =>
            (_currentMember?.IsOwner ?? true) ? Guild.Channels.Values :
            Guild.Channels.Values.Where(c => c.PermissionsFor(_currentMember).HasFlag(Permissions.AccessChannels));

    }
}
