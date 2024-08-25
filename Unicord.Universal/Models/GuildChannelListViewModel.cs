using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Messaging;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Models
{
    public class GuildChannelListViewModel : ViewModelBase
    {
        private bool _canEdit;

        public GuildChannelListViewModel(DiscordGuild guild)
            : base(null)
        {
            Guild = guild;
            InitialiseLists();
        }

        public DiscordGuild Guild { get; }
        public string Name => Guild.Name;
        public string HeaderImage => Guild.BannerUrl;
        public ObservableCollection<ChannelListViewModel> Channels { get; set; }
        public ListViewReorderMode ReorderMode => CanEdit ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;

        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                OnPropertySet(ref _canEdit, value);
                InvokePropertyChanged(nameof(ReorderMode));
            }
        }

        /// <summary>
        /// Initially populates the server list
        /// </summary>
        private void InitialiseLists()
        {
            Channels = null;

            var currentMember = Guild.CurrentMember;
            Debug.Assert(currentMember != null);

            var permissions = currentMember.PermissionsIn(null);
            CanEdit = permissions.HasPermission(Permissions.ManageChannels);

            var channels = Guild.Channels.Values;

            // Use new discord channel category behaviour (new as of 2017 KEKW)
            var orderedChannels = channels.Where(c => c.Type != ChannelType.Category)
                .Where(c => ShouldShowChannel(c, currentMember))
                .OrderBy(c => c.Type == ChannelType.Voice)
                .ThenBy(c => c.Position)
                .GroupBy(g => g.Parent)
                .OrderBy(g => g.Key?.Position)
                .SelectMany(g => g.Key != null ? g.Prepend(g.Key) : g)
                .Select(c => new ChannelListViewModel(c, this));

            Channels = new ObservableCollection<ChannelListViewModel>(orderedChannels);
        }

        private static bool ShouldShowChannel(DiscordChannel channel, DiscordMember currentMember)
        {
            if (currentMember.IsOwner)
                return true;

            return channel.IsCategory ?
                channel.Children.Any(x => x.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels)) :
                channel.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels);
        }
    }
}
