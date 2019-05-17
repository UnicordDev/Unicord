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
        private DiscordChannel _currentChannel;

        public DMChannelsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordDmChannel channel)
            {
                dmsList.SelectedItem = channel;
            }
            else
            {
                dmsList.SelectedIndex = -1;
            }
        }

        private void dmsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var channel = e.AddedItems.FirstOrDefault() as DiscordChannel;

            if (channel != null)
            {
                if (_currentChannel == channel)
                    return;

                _currentChannel = channel;

                this.FindParent<DiscordPage>()?.Navigate(channel, null);
            }
        }
    }
}
