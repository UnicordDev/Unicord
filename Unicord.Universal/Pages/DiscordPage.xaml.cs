using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Unicord.Universal.Controls.Messages;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Models.Voice;
using Unicord.Universal.Pages.Settings;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Services;
using Unicord.Universal.Shared;
using Unicord.Universal.Utilities;
//using Unicord.Universal.Voice;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace Unicord.Universal.Pages
{
    public sealed partial class DiscordPage : Page
    {
        public Frame MainFrame => mainFrame;
        public Frame LeftSidebarFrame => leftSidebarFrame;

        private MainPageArgs _args;
        private bool _loaded;

        internal DiscordPageViewModel Model { get; }
        internal bool IsWindowVisible { get; private set; }

        internal SwipeOpenHelper _helper;

        private bool _isPaneOpen => MainGridTransform.X != 0;

        public DiscordPage()
        {
            InitializeComponent();
            Model = DataContext as DiscordPageViewModel;

            _helper = new SwipeOpenHelper(Content, this, OpenPaneMobileStoryboard, ClosePaneMobileStoryboard);
            _helper.IsEnabled = false;

            IsWindowVisible = Window.Current.Visible;
            Window.Current.VisibilityChanged += Current_VisibilityChanged;

            //GuildsView.RegisterPropertyChangedCallback(MUXC.TreeView.SelectedItemProperty, )
            //this.AddAccelerator(Windows.System.VirtualKey.O, Windows.System.VirtualKeyModifiers.Control, (_, _) => Model.IsRightPaneOpen = !Model.IsRightPaneOpen);
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            IsWindowVisible = e.Visible;
            UpdateTitleBar();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateTitleBar();
            Analytics.TrackEvent("DiscordPage_NavigatedTo");

            if (e.Parameter is MainPageArgs args)
            {
                _args = args;

                if (_loaded && _args.ChannelId != 0)
                {
                    if (App.Discord.TryGetCachedChannel(_args.ChannelId, out var channel))
                    {
                        var service = DiscordNavigationService.GetForCurrentView();
                        await service.NavigateAsync(channel);
                    }
                }
            }
        }

        private void UpdateTitleBar()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                WindowingService.Current.HandleTitleBarForControl(sidebarMainGrid, true);
                WindowingService.Current.HandleTitleBarForControl(sidebarSecondaryGrid, true);
                WindowingService.Current.HandleTitleBarForControl(MainContent, true);
                TitleBarGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                WindowingService.Current.HandleTitleBarForControl(sidebarSecondaryGrid, true);
                WindowingService.Current.HandleTitleBarForControl(MainContent, true);
                TitleBarGrid.Visibility = Visibility.Visible;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.Discord == null)
                return; // im not 100% sure why this gets called on logout but it does so

            Analytics.TrackEvent("DiscordPage_Loaded");

            try
            {
                App.Discord.MessageCreated += Notification_MessageCreated;

                UpdateTitleBar();

                _loaded = true;

                this.FindParent<MainPage>().HideConnectingOverlay();

                var service = DiscordNavigationService.GetForCurrentView();
                if (_args != null && _args.ChannelId != 0 && App.Discord.TryGetCachedChannel(_args.ChannelId, out var channel))
                {
                    Analytics.TrackEvent("DiscordPage_NavigateToSpecifiedChannel");
                    await service.NavigateAsync(channel);
                }
                else
                {
                    Analytics.TrackEvent("DiscordPage_NavigateToFriendsPage");
                    Model.IsFriendsSelected = true;
                    LeftSidebarFrame.Navigate(typeof(DMChannelsPage));
                    MainFrame.Navigate(typeof(FriendsPage));
                }

                //var helper = SwipeOpenService.GetForCurrentView();
                //helper.AddAdditionalElement(SwipeHelper);

                var notificationService = BackgroundNotificationService.GetForCurrentView();
                await notificationService.StartupAsync();

                var possibleConnection = await VoiceConnectionModel.FindExistingConnectionAsync();
                if (possibleConnection != null)
                {
                    (DataContext as DiscordPageViewModel).VoiceModel = possibleConnection;
                }

                await ContactListManager.UpdateContactsListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync("An error has occured.", ex.Message);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Analytics.TrackEvent("DiscordPage_Unloaded");

            if (App.Discord != null)
            {
                App.Discord.MessageCreated -= Notification_MessageCreated;
            }
        }

        private async Task Notification_MessageCreated(MessageCreateEventArgs e)
        {
            if (!WindowingService.Current.IsChannelVisible(e.Channel.Id) && NotificationUtils.WillShowToast(e.Message) && IsWindowVisible)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShowNotification(e.Message));
            }
        }

        private void ShowNotification(DiscordMessage message)
        {
            notification.Show(new MessageControl() { MessageViewModel = new MessageViewModel(message) }, 7_000);
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
                _helper.Cancel();
                OpenPaneMobileStoryboard.Begin();
            }
        }

        public void CloseSplitPane()
        {
            if (ActualWidth <= 768 || MainGridTransform.X < 0)
            {
                _helper.Cancel();
                ClosePaneMobileStoryboard.Begin();
            }
        }

        private async void Notification_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var message = (((InAppNotification)sender).Content as MessageControl)?.MessageViewModel;
            if (message != null)
            {
                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(message.Channel);
            }
        }

        private async void UnreadDms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model.Navigating)
                return;

            if (e.AddedItems.FirstOrDefault() is DiscordDmChannel c)
            {
                await DiscordNavigationService.GetForCurrentView().NavigateAsync(c);
            }
        }

        private void mainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var service = FullscreenService.GetForCurrentView();
            if (service.IsFullscreenMode)
            {
                service.LeaveFullscreen();
            }
        }

        private async void friendsItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var service = DiscordNavigationService.GetForCurrentView();
            await service.NavigateAsync(null);
        }

        private async void guildsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            //var enumerable = ((DiscordPageModel)DataContext).Guilds.Select(g => g.Id).ToArray();
            //if (!enumerable.SequenceEqual(App.Discord.UserSettings.GuildPositions))
            //{
            //    await App.Discord.UpdateUserSettingsAsync(enumerable);
            //}
        }

        private async void SettingsItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var service = SettingsService.GetForCurrentView();
            await service.OpenAsync();
        }

        private void CloseItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CloseSplitPane();
        }

        private void CreateServerItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            return;

            var element = sender as FrameworkElement;
            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions"))
            {
                FlyoutBase.GetAttachedFlyout(element).ShowAt(element, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Right });
            }
            else
            {
                FlyoutBase.GetAttachedFlyout(element).ShowAt(element);
            }
        }

        private void FindServerIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void Self_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // _helper.IsEnabled = e.NewSize.Width <= 768;
        }

        private void ClydeLogo_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void TreeView_ItemInvoked(MUXC.TreeView sender, MUXC.TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is GuildListFolderViewModel viewModel)
            {
                viewModel.IsExpanded = !viewModel.IsExpanded;
            }

            if (args.InvokedItem is GuildListViewModel guildVM)
            {
                if (Model.Navigating)
                    return;

                if (!guildVM.Guild.IsUnavailable)
                {
                    LeftSidebarFrame.Navigate(typeof(GuildChannelListPage), guildVM.Guild);
                    Model.IsFriendsSelected = false;
                }
                else
                {
                    await UIUtilities.ShowErrorDialogAsync("ServerUnavailableTitle", "ServerUnavailableMessage");
                }
            }

            sender.SelectedItem = null;
        }
    }
}