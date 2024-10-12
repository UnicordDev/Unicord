using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Controls;
using Unicord.Universal.Misc;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.User;
using Unicord.Universal.Utilities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
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
    public sealed partial class UserListPage : Page
    {
        public UserListPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                progress.IsActive = true;
                viewSource.Source = null;

                if (e.Parameter is ChannelViewModel channel)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (channel.Channel is DiscordDmChannel dm)
                        {
                            viewSource.IsSourceGrouped = false;
                            viewSource.Source = dm.Recipients.OrderBy(r => r.DisplayName)
                               .Select(s => new UserViewModel(s, channel.Guild.Id));
                        }
                        else
                        {
                            viewSource.IsSourceGrouped = true;
                            viewSource.Source = channel.Channel.Users
                               .Distinct()
                               .OrderBy(g => g.DisplayName)
                               .Select(s => new UserViewModel(s, channel.Guild.Id))
                               .GroupBy(g => g.Member.Roles.OrderByDescending(r => r.Position).FirstOrDefault(r => r.IsHoisted))
                               .OrderByDescending(g => g.Key?.Position);
                        }

                        progress.IsActive = false;
                    });
                }
            }
            catch
            {
                // TODO: Log
            }
        }

        private void userList_ItemClick(object sender, ItemClickEventArgs e)
        {
            //var item = e.ClickedItem;
            //if (item != null)
            //{
            //    var element = (sender as ListView).ContainerFromItem(item);
            //    //AdaptiveFlyoutUtilities.ShowAdaptiveFlyout<UserFlyout>(item, element as FrameworkElement);
            //    userList.SelectedItem = null;
            //}
        }
    }
}
