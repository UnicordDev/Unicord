using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Models
{
    public class GuildChannelListViewModel : NotifyPropertyChangeImpl, IDisposable
    {
        //public class ChannelViewModel : INotifyPropertyChanged, IDisposable
        //{
        //    private DiscordChannel _channel;

        //    public ChannelViewModel(DiscordChannel channel)
        //    {
        //        _channel = channel;
        //    }

        //    public event PropertyChangedEventHandler PropertyChanged
        //    {
        //        add => _channel.PropertyChanged += value;
        //        remove => _channel.PropertyChanged -= value;
        //    }

        //    public void Dispose()
        //    {

        //    }
        //}

        private bool _canEdit;
        private SynchronizationContext _syncContext;

        public GuildChannelListViewModel(DiscordGuild guild)
        {
            Guild = guild;
            _syncContext = SynchronizationContext.Current;

            InitialiseLists();

            //App.Discord.ChannelCreated += Discord_ChannelCreated;
            //App.Discord.ChannelUpdated += Discord_ChannelUpdated;
            //App.Discord.ChannelDeleted += Discord_ChannelDeleted;
        }

        public DiscordGuild Guild { get; }
        public string Name => Guild.Name;
        public string HeaderImage => Guild.BannerUrl;
        public ObservableCollection<DiscordChannel> Channels { get; set; }
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
            var permissions = currentMember.PermissionsIn(null);
            CanEdit = permissions.HasPermission(Permissions.ManageChannels);

            // var channels = Guild.Channels.Values;
            var channels = Guild.Channels.Values;
            var maxPos = channels.Max(c => c.Position) + 1;

            // Use new discord channel category behaviour (new as of 2017 KEKW)
            var orderedChannels = channels.Where(c => c.Type != ChannelType.Category)
                .Where(c => ShouldShowChannel(c, currentMember))
                .OrderBy(c => c.Type == ChannelType.Voice)
                .ThenBy(c => c.Position)
                .GroupBy(g => g.Parent)
                .OrderBy(g => g.Key?.Position)
                .SelectMany(g => g.Key != null ? g.Prepend(g.Key) : g);

            Channels = new ObservableCollection<DiscordChannel>(orderedChannels);
        }

        private static bool ShouldShowChannel(DiscordChannel channel, DiscordMember currentMember)
        {
            if (currentMember.IsOwner)
                return true;

            return channel.IsCategory ?
                channel.Children.Any(x => x.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels)) :
                channel.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels);
        }

        public void Dispose()
        {

        }
    }
}
