using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class FriendsPage : Page
    {
        public FriendsPage()
        {
            InitializeComponent();
        }
      
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var gridView = (sender as GridView);

            foreach (var i in e.AddedItems)
            {
                if (gridView.ItemsPanelRoot.Children.FirstOrDefault(c => (c as GridViewItem)?.Content == i) is GridViewItem cont)
                {
                    gridView.SelectedItem = null;
                    this.FindParent<MainPage>().ShowUserOverlay((i as DiscordRelationship).User, true);
                }
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
