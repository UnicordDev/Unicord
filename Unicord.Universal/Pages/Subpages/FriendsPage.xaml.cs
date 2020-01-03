using System.Linq;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Unicord.Universal.Behaviours;
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

            //ListViewBehaviour.SetMinItemWidth(all, 300);
            //ListViewBehaviour.SetMinItemWidth(online, 300);
            //ListViewBehaviour.SetMinItemWidth(pending, 300);
            //ListViewBehaviour.SetMinItemWidth(blocked, 300);

            //if (ReorderGridAnimation.IsSupported)
            //{
            //    ReorderGridAnimation.SetDuration(all, 250);
            //    ReorderGridAnimation.SetDuration(online, 250);
            //    ReorderGridAnimation.SetDuration(pending, 250);
            //    ReorderGridAnimation.SetDuration(blocked, 250);
            //}
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is DiscordRelationship rel)
            {
                this.FindParent<MainPage>().ShowUserOverlay(rel.User, true);
            }
        }

        private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var grid = sender as FrameworkElement;
            var enter = grid.Resources["pointerEntered"] as Storyboard;
            var exited = grid.Resources["pointerLeft"] as Storyboard;

            exited?.Stop();
            enter?.Begin();
        }

        private void Grid_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var grid = sender as FrameworkElement;
            var enter = grid.Resources["pointerEntered"] as Storyboard;
            var exited = grid.Resources["pointerLeft"] as Storyboard;

            enter?.Stop();
            exited?.Begin();
        }

        private void Grid_PointerCanceled(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var grid = sender as FrameworkElement;
            var enter = grid.Resources["pointerEntered"] as Storyboard;
            var exited = grid.Resources["pointerLeft"] as Storyboard;

            enter?.Stop();
            exited?.Begin();
        }

        private void ShowSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            var page = this.FindParent<DiscordPage>();
            if (page != null)
            {
                page.ToggleSplitPane();
            }
        }
    }
}
