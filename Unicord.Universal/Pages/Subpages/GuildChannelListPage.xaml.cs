using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Linq;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class GuildChannelListPage : Page
    {
        private bool _suspend = false;
        public DiscordGuild Guild { get; private set; }

        public GuildChannelListPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Guild = e.Parameter as DiscordGuild;
            DataContext = new GuildChannelListViewModel(Guild);
        }

        private async void channelsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = e.AddedItems.FirstOrDefault() as ChannelListViewModel;
            if (viewModel != null && !_suspend)
            {
                if (viewModel.ChannelType != ChannelType.Text && viewModel.ChannelType != ChannelType.Announcement)
                {
                    channelsList.SelectedItem = e.RemovedItems.FirstOrDefault();

                    if (viewModel.ChannelType != ChannelType.Voice)
                        return;
                }

                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(viewModel.Channel);
            }
        }

        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var flyout = FlyoutBase.GetAttachedFlyout(Header);
            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions"))
            {
                var options = new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
                flyout.ShowAt(btn, options);
            }
            else
            {
                flyout.ShowAt(Header);
            }
        }

        internal void SetSelectedChannel(DiscordChannel channel)
        {
            _suspend = true;
            var viewModel = (GuildChannelListViewModel)DataContext;
            var channelVM = viewModel.Channels.FirstOrDefault(c => c.Channel == channel);

            channelsList.SelectedItem = channelVM;
            _suspend = false;
        }
    }
}
