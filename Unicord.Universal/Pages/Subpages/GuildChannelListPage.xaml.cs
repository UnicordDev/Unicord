using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
            Tag = Guild.Name;

            if (DataContext is GuildChannelListViewModel model)
            {
                model.Dispose();
            }

            DataContext = new GuildChannelListViewModel(Guild);
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
