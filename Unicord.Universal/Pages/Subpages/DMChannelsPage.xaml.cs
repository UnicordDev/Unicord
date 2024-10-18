using System.Linq;
using DSharpPlus.Entities;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class DMChannelsPage : Page
    {
        private DMChannelsViewModel _model;

        public DMChannelsPage()
        {
            InitializeComponent();
            _model = DataContext as DMChannelsViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _model.UpdatingIndex = true;

            if (e.Parameter is DiscordDmChannel channel)
            {
                var model = _model.DMChannels.FirstOrDefault(m => m.Channel == channel);
                if (model is not null)
                    _model.SelectedIndex = _model.DMChannels.IndexOf(model);
            }
            else
            {
                _model.SelectedIndex = -1;
            }

            _model.UpdatingIndex = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _model.SelectedIndex = -1;
        }

        private async void dmsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataContext = DataContext as DMChannelsViewModel;
            if (dataContext.UpdatingIndex)
                return;

            var channel = e.AddedItems.FirstOrDefault() as ChannelViewModel;
            if (channel != null)
            {
                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(channel.Channel);
            }
        }
    }
}
