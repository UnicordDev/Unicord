using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Subpages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GuildChannelsPage : Page
    {
        public DiscordGuild Guild { get; private set; }

        public GuildChannelsPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Guild = e.Parameter as DiscordGuild;
            Tag = Guild.Name;
            DataContext = Guild;

            RefreshList();
        }

        private void RefreshList()
        {
            cvs.Source = null;
            ring.IsActive = true;

            channelsList.SelectionChanged -= channelsList_SelectionChanged;
            var permissions = Guild.CurrentMember.PermissionsIn(Guild.Channels.Values.FirstOrDefault());
            if (permissions.HasPermission(Permissions.ManageChannels))
            {
                channelsList.ReorderMode = ListViewReorderMode.Enabled;
                channelsList.CanDrag = true;
                channelsList.CanDragItems = true;
                channelsList.CanReorderItems = true;
                channelsList.AllowDrop = true;
            }
            else
            {
                channelsList.ReorderMode = ListViewReorderMode.Disabled;
                channelsList.CanDrag = false;
                channelsList.CanDragItems = false;
                channelsList.CanReorderItems = false;
                channelsList.AllowDrop = false;
            }

            PopulateChannelList(cvs, Guild);

            channelsList.SelectionChanged += channelsList_SelectionChanged;

            ring.IsActive = false;
        }

        public static void PopulateChannelList(CollectionViewSource cvs, DiscordGuild guild, bool excludeVoice = false)
        {
            if (guild.Channels.Any())
            {
                var currentMember = guild.CurrentMember;

                var channels = guild.IsOwner ?
                    guild.Channels.Values :
                    guild.Channels.Values.Where(c => c.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels));

                var ownerId = guild.OwnerId;

                if (excludeVoice)
                {
                    channels = channels.Where(c => c.Type != ChannelType.Voice);
                }

                if (channels.Any(c => c.IsCategory))
                {
                    // Use new discord channel category behaviour
                    cvs.IsSourceGrouped = true;
                    cvs.Source = channels
                        .Where(c => !c.IsCategory)
                        .OrderBy(c => c.Type)
                        .ThenBy(c => c.Position)
                        .GroupBy(g => g.Parent)
                        .OrderBy(c => c.Key?.Position);
                }
                else
                {
                    // Use old discord non-category behaviour
                    cvs.IsSourceGrouped = false;
                    cvs.Source = channels
                        .OrderBy(c => c.Type)
                        .ThenBy(c => c.Position);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App.Discord.ChannelCreated += Discord_ChannelCreated;
            App.Discord.ChannelUpdated += Discord_ChannelUpdated;
            App.Discord.ChannelDeleted += Discord_ChannelDeleted;
        }

        private async Task Discord_ChannelCreated(ChannelCreateEventArgs e)
        {
            if (e.Guild.Id == Guild.Id)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RefreshList());
            }
        }

        private async Task Discord_ChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (e.Guild.Id == Guild.Id)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RefreshList());
            }
        }

        private async Task Discord_ChannelDeleted(ChannelDeleteEventArgs e)
        {
            if (e.Guild.Id == Guild.Id)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RefreshList());
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.Discord != null)
            {
                App.Discord.ChannelCreated -= Discord_ChannelCreated;
                App.Discord.ChannelUpdated -= Discord_ChannelUpdated;
                App.Discord.ChannelDeleted -= Discord_ChannelDeleted;
            }

            cvs.Source = null;
            cvs.IsSourceGrouped = false;
        }

        private void channelsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var channel = e.AddedItems.FirstOrDefault() as DiscordChannel;

            if (channel != null)
            {
                this.FindParent<DiscordPage>().Navigate(channel, null);
            }
        }
    }
}
