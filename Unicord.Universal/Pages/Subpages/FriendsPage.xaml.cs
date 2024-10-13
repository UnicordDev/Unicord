using System;
using System.Linq;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Unicord.Universal.Behaviours;
using Unicord.Universal.Models.Relationships;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class FriendsPage : Page
    {
        public FriendsPage()
        {
            InitializeComponent();

#if !(ARM && DEBUG)
            ListViewBehaviour.SetMinItemWidth(AllView, 300);
            ListViewBehaviour.SetMinItemWidth(OnlineView, 300);
            ListViewBehaviour.SetMinItemWidth(PendingView, 300);
            ListViewBehaviour.SetMinItemWidth(BlockedView, 300);

            ItemsReorderAnimation.SetDuration(AllView, TimeSpan.FromMilliseconds(250));
            ItemsReorderAnimation.SetDuration(OnlineView, TimeSpan.FromMilliseconds(250));
            ItemsReorderAnimation.SetDuration(PendingView, TimeSpan.FromMilliseconds(250));
            ItemsReorderAnimation.SetDuration(BlockedView, TimeSpan.FromMilliseconds(250));
#endif
        }

        private async void OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is RelationshipViewModel rel)
            {
                //this.FindParent<MainPage>().ShowUserOverlay(rel.User, true);
                await OverlayService.GetForCurrentView()
                    .ShowOverlayAsync<UserInfoOverlayPage>(rel.User);
            }
        }

        private void ShowSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            SplitPaneService.GetForCurrentView()
                .ToggleLeftPane();
        }
    }
}
