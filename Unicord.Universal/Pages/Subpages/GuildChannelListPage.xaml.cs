using DSharpPlus;
using DSharpPlus.Entities;
using System.Linq;
using Unicord.Universal.Models;
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
        public DiscordGuild Guild { get; private set; }

        public GuildChannelListPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Guild = e.Parameter as DiscordGuild;
            if (DataContext is GuildChannelListViewModel model)
            {
                model.Dispose();
            }

            DataContext = new GuildChannelListViewModel(Guild);
        }

        private async void channelsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var channel = e.AddedItems.FirstOrDefault() as DiscordChannel;
            if (channel != null)
            {
                if (channel.Type != ChannelType.Text && channel.Type != ChannelType.News)
                {
                    channelsList.SelectedItem = e.RemovedItems.FirstOrDefault();

                    if (channel.Type != ChannelType.Voice)
                        return;
                }

                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(channel);
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
    }
}
