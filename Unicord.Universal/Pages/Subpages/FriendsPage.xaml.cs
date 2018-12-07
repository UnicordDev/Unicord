using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Subpages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FriendsPage : Page
    {
        private ObservableCollection<DiscordRelationship> _all = new ObservableCollection<DiscordRelationship>();
        private ObservableCollection<DiscordRelationship> _online = new ObservableCollection<DiscordRelationship>();
        private ObservableCollection<DiscordRelationship> _pending = new ObservableCollection<DiscordRelationship>();
        private ObservableCollection<DiscordRelationship> _blocked = new ObservableCollection<DiscordRelationship>();

        public FriendsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var allList = new List<DiscordRelationship>();
                var onlineList = new List<DiscordRelationship>();

                foreach (var rel in App.Discord.Relationships.OrderBy(r => r.User?.Username))
                {
                    switch (rel.RelationshipType)
                    {
                        case DiscordRelationshipType.Friend:
                            allList.Add(rel);

                            if (rel.User.Presence != null && rel.User.Presence.Status != UserStatus.Offline)
                            {
                                onlineList.Add(rel);
                            }
                            break;
                        case DiscordRelationshipType.Blocked:
                            _blocked.Add(rel);
                            break;
                        case DiscordRelationshipType.IncomingRequest:
                        case DiscordRelationshipType.OutgoingRequest:
                            _pending.Add(rel);
                            break;
                        default:
                            break;
                    }
                }

                _all = new ObservableCollection<DiscordRelationship>(allList);
                _online = new ObservableCollection<DiscordRelationship>(onlineList);

                all.ItemsSource = _all;
                online.ItemsSource = _online;
                pending.ItemsSource = _pending;
                blocked.ItemsSource = _blocked;

                App.Discord.RelationshipAdded += Discord_RelationshipAdded;
                App.Discord.RelationshipRemoved += Discord_RelationshipRemoved;
                App.Discord.PresenceUpdated += Discord_PresenceUpdated;
            }
            catch { }
        }

        private async Task Discord_PresenceUpdated(PresenceUpdateEventArgs e)
        {
            if (e.PresenceBefore?.Status != e.Status)
            {
                var rel = App.Discord.Relationships.FirstOrDefault(r => r.User.Id == e.User.Id);
                if (rel != null && rel.RelationshipType == DiscordRelationshipType.Friend)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SortRelationship(rel, true);
                    });
                }
            }
        }

        private async Task Discord_RelationshipAdded(RelationshipEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SortRelationship(e.Relationship));
        }

        private async Task Discord_RelationshipRemoved(RelationshipEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RemoveRelationship(e.Relationship));
        }

        private void SortRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            RemoveRelationship(rel, skipAll);

            switch (rel.RelationshipType)
            {
                case DiscordRelationshipType.Friend:
                    int i;

                    if (!skipAll)
                    {
                        i = _all.ToList().BinarySearch(rel);
                        if (i < 0) i = ~i;
                        _all.Insert(i, rel);
                    }

                    if (rel.User.Presence != null && rel.User.Presence.Status != UserStatus.Offline)
                    {
                        i = _online.ToList().BinarySearch(rel);
                        if (i < 0) i = ~i;

                        _online.Insert(i, rel);
                    }
                    break;
                case DiscordRelationshipType.Blocked:
                    _blocked.Add(rel);
                    break;
                case DiscordRelationshipType.IncomingRequest:
                case DiscordRelationshipType.OutgoingRequest:
                    _pending.Add(rel);
                    break;
                default:
                    break;
            }
        }

        private void RemoveRelationship(DiscordRelationship rel, bool skipAll = false)
        {
            if (!skipAll)
                _all.Remove(rel);
            _online.Remove(rel);
            _pending.Remove(rel);
            _blocked.Remove(rel);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.Discord != null)
            {
                App.Discord.RelationshipAdded -= Discord_RelationshipAdded;
                App.Discord.RelationshipRemoved -= Discord_RelationshipRemoved;
                App.Discord.PresenceUpdated -= Discord_PresenceUpdated;
            }
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

            foreach (var i in e.RemovedItems)
            {
                if (gridView.ItemsPanelRoot.Children.FirstOrDefault(c => (c as GridViewItem)?.Content == i) is GridViewItem cont)
                {

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
