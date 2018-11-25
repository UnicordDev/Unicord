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

        private Task Discord_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Channel is DiscordDmChannel dm)
            {
                var index = _dms.IndexOf(e.Message.Channel as DiscordDmChannel);

                if (index > 0)
                {
                    return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _dms.Move(index, 0)).AsTask();
                }
                else if (index < 0)
                {
                    return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _dms.Insert(0, dm)).AsTask();
                }
            }

            return Task.CompletedTask;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Discord.DmChannelCreated -= Discord_DmChannelCreated;
            App.Discord.DmChannelDeleted -= Discord_DmChannelDeleted;
            App.Discord.MessageCreated -= Discord_MessageCreated;
        }

        private void dmsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var channel = e.AddedItems.FirstOrDefault() as DiscordChannel;

            if (channel != null)
            {
                this.FindParent<DiscordPage>().Frame.Navigate(typeof(ChannelPage), channel);
            }
        }
    }
}
