using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.UI;
using Unicord.Universal.Models;
using Unicord.Universal.Services;
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

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class DMChannelsPage : Page
    {
        private DiscordDmChannel _currentChannel;

        public DMChannelsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordDmChannel channel)
            {
                _currentChannel = channel;
                dmsList.SelectedItem = channel;
            }
            else
            {
                _currentChannel = null;
                dmsList.SelectedIndex = -1;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _currentChannel = null;
            dmsList.SelectedIndex = -1;
        }

        private async void dmsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataContext = DataContext as DMChannelsViewModel;
            if (dataContext.UpdatingIndex)
                return;

            var channel = e.AddedItems.FirstOrDefault() as DiscordChannel;
            if (channel != null)
            {
                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(channel);
            }
        }
    }
}
