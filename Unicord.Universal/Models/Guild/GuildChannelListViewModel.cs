using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Models.Guild
{
    public class GuildChannelListViewModel : GuildViewModel
    {
        private bool _canEdit;

        public GuildChannelListViewModel(DiscordGuild guild)
            : base(guild.Id)
        {
            Channels = [];
            InitialiseLists();

            WeakReferenceMessenger.Default.Register<GuildChannelListViewModel, ReadyEventArgs>(this, (t, e) => t.OnReady(e.Event));
        }

        public string HeaderImage => Guild.GetBannerUrl();
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

        private Task OnReady(ReadyEventArgs e)
        {
            syncContext.Post(o => InitialiseLists(), null);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initially populates the server list
        /// </summary>
        private void InitialiseLists()
        {
            Channels.Clear();

            var currentMember = Guild.CurrentMember;
            Debug.Assert(currentMember != null);

            var permissions = currentMember.PermissionsIn(null);
            CanEdit = permissions.HasPermission(Permissions.ManageChannels);

            var channels = Guild.Channels;
            static bool FilterChannels(DiscordChannel channel, DiscordMember currentMember)
            {
                if (currentMember.IsOwner)
                    return true;

                return channel.IsCategory ?
                    channel.Children.Any(x => x.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels)) :
                    channel.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels);
            }

            static bool FilterThreads(DiscordThreadChannel channel)
            {
                var currentUserId = DiscordManager.Discord.CurrentUser.Id;
                return (channel.CurrentMember != null || 
                        channel.CreatorId == currentUserId ||
                        (channel.MemberIdsPreview != null && channel.MemberIdsPreview.Contains(currentUserId))) 
                    && !(channel.ThreadMetadata?.IsArchived ?? true);
            }

            // Use new discord channel category behaviour (new as of 2017 KEKW)
            var orderedChannels = channels.Select(t => t.Value)
                .Where(c => c.Type != ChannelType.Category)
                .Where(c => FilterChannels(c, currentMember))
                .OrderBy(c => c.Type == ChannelType.Voice)
                .ThenBy(c => c.Position)
                .GroupBy(g => g.Parent)
                .OrderBy(g => g.Key?.Position)
                .SelectMany(g => g.Key != null ? g.Prepend(g.Key) : g)
                .SelectMany<DiscordChannel, DiscordChannel>(c => [c, .. c.Threads.Where(FilterThreads).Cast<DiscordChannel>()])
                .Select(c => new ChannelListViewModel(c, this));

            foreach (var channel in orderedChannels)
                Channels.Add(channel);
        }
    }
}
