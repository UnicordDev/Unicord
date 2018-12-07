using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unicord.Universal.Controls;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Settings;
using Unicord.Universal.Pages.Subpages;
using Windows.ApplicationModel.Contacts;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DiscordPage : Page
    {
        public new Frame Frame => mainFrame;
        private ObservableCollection<DiscordGuild> _guilds = new ObservableCollection<DiscordGuild>();
        private MainPageEventArgs _args;
        private bool _loaded;
        private bool _visibility;
        private bool _isPaneOpen => ContentTransform.X != 0;

        public DiscordPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            _visibility = Window.Current.Visible;
            Window.Current.VisibilityChanged += Current_VisibilityChanged;
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            _visibility = e.Visible;
            UpdateTitleBar();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            App.Discord.MessageCreated += Notification_MessageCreated;

            UpdateTitleBar();

            if (e.Parameter is MainPageEventArgs args)
            {
                _args = args;

                if (_loaded)
                {
                    var channel = await App.Discord.GetChannelAsync(_args.ChannelId);
                    Navigate(channel, new DrillInNavigationTransitionInfo());
                }
            }
        }

        private void UpdateTitleBar()
        {
            if (App.StatusBarFill != default)
            {
                if (App.StatusBarFill.Top > 25)
                {
                    iconGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    sidebarMainGrid.Padding = App.StatusBarFill;
                    sidebarSecondaryGrid.Padding = App.StatusBarFill;
                }
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckSettingsPane();

                _loaded = true;

                var guildPositions = App.Discord.UserSettings?.GuildPositions;
                foreach (var guild in App.Discord.Guilds.Values.OrderBy(g => guildPositions.IndexOf(g.Id)))
                {
                    _guilds.Add(guild);
                }

                App.Discord.UserSettingsUpdated += Discord_UserSettingsUpdated;

                this.FindParent<MainPage>().HideConnectingOverlay();

                //if (App.RoamingSettings.Read(Constants.SYNC_CONTACTS, true))
                //{
                //    try
                //    {
                //        var contacts = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                //        if (contacts != null)
                //        {
                //            App.Discord.RelationshipAdded += Discord_RelationshipAdded;
                //            App.Discord.RelationshipRemoved += Discord_RelationshipRemoved;
                //            App.CanAccessContacts = true;
                //        }
                //    }
                //    catch
                //    {
                //        App.CanAccessContacts = false;
                //    }
                //}

                if (_args != null)
                {
                    var channel = await App.Discord.GetChannelAsync(_args.ChannelId);
                    Navigate(channel, new DrillInNavigationTransitionInfo());
                }
                else
                {
                    friendsItem.IsSelected = true;
                    friendsItem_Tapped(null, null);
                }

            }
            catch { }
        }

        private async Task Discord_RelationshipAdded(RelationshipEventArgs e)
        {
            //if (e.Relationship.RelationshipType == DiscordRelationshipType.Friend)
            //{
            //    var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions
            //    if (store != null)
            //    {
            //        var lists = await store.FindContactListsAsync();
            //        var list = lists.FirstOrDefault() ?? (await store.CreateContactListAsync("Unicord"));
            //        await Contacts.AddOrUpdateContactForRelationship(list, e.Relationship);
            //    }
            //}
        }

        private async Task Discord_RelationshipRemoved(RelationshipEventArgs e)
        {
            var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions
            if (store != null)
            {
                var lists = await store.FindContactListsAsync();
                var list = lists.FirstOrDefault() ?? (await store.CreateContactListAsync("Unicord"));

                var contact = await list.GetContactFromRemoteIdAsync($"Unicord_{e.Relationship.Id}");
                if (contact != null)
                {
                    await list.DeleteContactAsync(contact);
                }
            }
        }

        private async Task Discord_UserSettingsUpdated(UserSettingsUpdateEventArgs e)
        {
            var guildPositions = App.Discord.UserSettings?.GuildPositions;
            if (!_guilds.Select(g => g.Id).SequenceEqual(guildPositions))
            {
                for (var i = 0; i < guildPositions.Count; i++)
                {
                    var id = guildPositions[i];
                    var guild = _guilds[i];
                    if (id != guild.Id)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            _guilds.Move(_guilds.IndexOf(_guilds.First(g => g.Id == id)), i);
                        });
                    }
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.Discord != null)
            {
                App.Discord.UserSettingsUpdated -= Discord_UserSettingsUpdated;
            }
        }

        private async Task Notification_MessageCreated(MessageCreateEventArgs e)
        {
            if (SharedTools.WillShowToast(e.Message) && App._currentChannelId != e.Channel.Id)
            {
                if (_visibility)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShowNotification(e.Message));
                }
                else
                {
                    var notification = Tools.GetWindows10Toast(e.Message,
                        Tools.GetMessageTitle(e.Message),
                        Tools.GetMessageContent(e.Message));

                    var toastNotifier = ToastNotificationManager.CreateToastNotifier();
                    toastNotifier.Show(notification);
                }
            }
        }

        public void ToggleSplitPane()
        {
            if (_isPaneOpen)
            {
                CloseSplitPane();
            }
            else
            {
                OpenSplitPane();
            }
        }

        public void OpenSplitPane()
        {
            if (ActualWidth <= 768)
            {
                OpenPaneMobileStoryboard.Begin();
            }
        }

        public void CloseSplitPane()
        {
            if (ActualWidth <= 768)
            {
                ClosePaneMobileStoryboard.Begin();
            }
        }

        private void ShowNotification(DiscordMessage message)
        {
            notification.Content = new MessageViewer() { Message = message, IsEnabled = false };
            notification.Show(7_000);
        }

        private void Notification_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var message = ((sender as InAppNotification).Content as MessageViewer)?.Message;
            if (message != null)
                Navigate(message.Channel, new DrillInNavigationTransitionInfo());
        }

        private void guildsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is DiscordGuild g)
            {
                sidebarFrame.Navigate(typeof(GuildChannelsPage), g);
                friendsItem.IsSelected = false;
            }
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // CloseSplitPane();
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            //sidebarFrame.GoBack();
        }

        internal async void Navigate(DiscordChannel channel, NavigationTransitionInfo info)
        {
            CloseSplitPane();

            guildsList.SelectionChanged -= guildsList_SelectionChanged;

            friendsItem.IsSelected = false;
            guildsList.SelectedIndex = -1;

            if (channel is DiscordDmChannel && !(sidebarFrame.Content is DMChannelsPage))
            {
                friendsItem.IsSelected = true;
                sidebarFrame.Navigate(typeof(DMChannelsPage), channel, new DrillInNavigationTransitionInfo());
            }
            else if (channel.Guild != null && (!(sidebarFrame.Content is GuildChannelsPage p) || p.Guild != channel.Guild))
            {
                guildsList.SelectedItem = channel.Guild;
                sidebarFrame.Navigate(typeof(GuildChannelsPage), channel.Guild, new DrillInNavigationTransitionInfo());
            }
            if (channel.IsNSFW)
            {
                if (await WindowsHello.VerifyAsync(Constants.VERIFY_NSFW, "Verify your identity to access this channel"))
                {
                    if (!App.RoamingSettings.Read($"NSFW_{channel.Id}", false) || !App.RoamingSettings.Read($"NSFW_All", false))
                    {
                        Frame.Navigate(typeof(ChannelWarningPage), channel, info ?? new SlideNavigationTransitionInfo());
                    }
                    else
                    {
                        Frame.Navigate(typeof(ChannelPage), channel, info ?? new SlideNavigationTransitionInfo());
                    }
                }
            }
            else
            {
                Frame.Navigate(typeof(ChannelPage), channel, info ?? new SlideNavigationTransitionInfo());
            }

            guildsList.SelectionChanged += guildsList_SelectionChanged;
        }

        private void mainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                var page = this.FindParent<MainPage>();
                if (page != null)
                {
                    page.LeaveFullscreen();
                }
                view.ExitFullScreenMode();
            }
        }

        private void friendsItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            friendsItem.IsSelected = true;
            guildsList.SelectedItem = null;
            if (!(sidebarFrame.Content is DMChannelsPage))
            {
                sidebarFrame.Navigate(typeof(DMChannelsPage), null, new DrillInNavigationTransitionInfo());
            }

            if (!(mainFrame.Content is FriendsPage) || (mainFrame.Content is ChannelPage cp && cp.ViewModel?.Channel.IsPrivate == true))
            {
                mainFrame.Navigate(typeof(FriendsPage), null, new SlideNavigationTransitionInfo());
            }
        }

        private async void guildsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            var enumerable = _guilds.Select(g => g.Id);
            if (!enumerable.SequenceEqual(App.Discord.UserSettings.GuildPositions))
            {
                await App.Discord.UpdateUserSettingsAsync(enumerable.ToArray());
            }
        }

        private void CheckSettingsPane()
        {
            if (ActualWidth <= 768)
            {
                MobileHeightAnimation.To = ActualHeight;
                SettingsPaneTransform.Y = ActualHeight;
            }
            else
            {
                SettingsContainer.Width = 450;
                SettingsPaneTransform.X = 450;
                SettingsContainer.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }

        private async void SettingsItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (await WindowsHello.VerifyAsync(Constants.VERIFY_SETTINGS, "Verify your identity to open settings."))
            {
                SettingsOverlayGrid.Visibility = Visibility.Visible;

                CheckSettingsPane();

                if (ActualWidth > 768)
                {
                    OpenSettingsDesktopStoryboard.Begin();
                }
                else
                {
                    OpenSettingsMobileStoryboard.Begin();
                }
            }

            //SettingsGrid.Navigate(typeof(SettingsPage), null, new SuppressNavigationTransitionInfo());
        }

        private void SettingsOverlayBackground_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CloseSettings();
        }

        internal void CloseSettings()
        {
            if (ActualWidth > 768)
            {
                CloseSettingsDesktopStoryboard.Begin();
            }
            else
            {
                CloseSettingsMobileStoryboard.Begin();
            }
        }

        private void CloseSettingsStoryboard_Completed(object sender, object e)
        {
            SettingsOverlayGrid.Visibility = Visibility.Collapsed;
        }

        private void CloseItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CloseSplitPane();
        }
    }
}
