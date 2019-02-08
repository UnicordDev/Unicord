using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class DMChannelsPage : Page
    {
        private ObservableCollection<DiscordDmChannel> _dms = new ObservableCollection<DiscordDmChannel>();

        public DMChannelsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordDmChannel channel)
            {
                dmsList.SelectionChanged -= dmsList_SelectionChanged;
                dmsList.SelectedItem = channel;
                dmsList.SelectionChanged += dmsList_SelectionChanged;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _dms = new ObservableCollection<DiscordDmChannel>(App.Discord.PrivateChannels.OrderByDescending(r => r.ReadState?.LastMessageId));

            App.Discord.DmChannelCreated += Discord_DmChannelCreated;
            App.Discord.DmChannelDeleted += Discord_DmChannelDeleted;
            App.Discord.MessageCreated += Discord_MessageCreated;

            dmsList.ItemsSource = _dms;
        }

        private Task Discord_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _dms.Insert(0, e.Channel))
                .AsTask();
        }

        private Task Discord_DmChannelDeleted(DmChannelDeleteEventArgs e)
        {
            return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _dms.Remove(e.Channel))
                .AsTask();
        }

        private async Task Discord_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Channel is DiscordDmChannel dm)
            {
                var index = _dms.IndexOf(e.Message.Channel as DiscordDmChannel);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    dmsList.SelectionChanged -= dmsList_SelectionChanged;
                    var selected = dmsList.SelectedIndex == index;

                    if (index > 0)
                    {
                        _dms.Move(index, 0);
                    }
                    else if (index < 0)
                    {
                        _dms.Insert(0, dm);
                    }

                    if (selected)
                        dmsList.SelectedIndex = 0;

                    dmsList.SelectionChanged += dmsList_SelectionChanged;
                });
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.Discord != null)
            {
                App.Discord.DmChannelCreated -= Discord_DmChannelCreated;
                App.Discord.DmChannelDeleted -= Discord_DmChannelDeleted;
                App.Discord.MessageCreated -= Discord_MessageCreated;
            }
        }

        private void dmsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var channel = e.AddedItems.FirstOrDefault() as DiscordChannel;

            if (channel != null)
            {
                this.FindParent<DiscordPage>().Navigate(channel, null);
            }
        }
    }
}
