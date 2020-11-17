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
using Unicord.Universal.Pages.Settings;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Unicord.Universal.Voice;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using static Unicord.Constants;

namespace Unicord.Universal.Pages
{
    public sealed partial class DiscordPage : Page
    {
        public Frame MainFrame => mainFrame;
        public Frame SidebarFrame => sidebarFrame;

        private MainPageArgs _args;
        private bool _loaded;

        internal DiscordPageModel Model { get; }
        internal bool IsWindowVisible { get; private set; }

        internal SwipeOpenHelper _helper;

        private bool _isPaneOpen => ContentTransform.X != 0;

        public DiscordPage()
        {
            InitializeComponent();
            Model = DataContext as DiscordPageModel;

            _helper = new SwipeOpenHelper(Content, this, OpenPaneMobileStoryboard, ClosePaneMobileStoryboard);

            IsWindowVisible = Window.Current.Visible;
            Window.Current.VisibilityChanged += Current_VisibilityChanged;
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
                WindowManager.HandleTitleBarForControl(sidebarMainGrid);
                WindowManager.HandleTitleBarForControl(sidebarSecondaryGrid);
            }
            else
            {
                iconGrid.Visibility = Visibility.Visible;
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
                CheckSettingsPane();

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
                    SidebarFrame.Navigate(typeof(DMChannelsPage));
                    MainFrame.Navigate(typeof(FriendsPage));
                }

                if (_args?.ThemeLoadException != null)
                {
                    Analytics.TrackEvent("DiscordPage_ThemeErrorMessageShown");

                    var message = App.Discord.CreateMockMessage(
                        $"We had some trouble loading your selected themes, so we disabled them for this launch. For more information, see settings.",
                        App.Discord.CreateMockUser("Unicord", "CORD"));
                    ShowNotification(message);
                }

                await StartBackgroundTaskAsync();

                var possibleConnection = await VoiceConnectionModel.FindExistingConnectionAsync();
                if (possibleConnection != null)
                {
                    (DataContext as DiscordPageModel).VoiceModel = possibleConnection;
                }

                await ContactListManager.UpdateContactsListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync("An error has occured.", ex.Message);
            }
        }

        private async Task RegisterBackgroundTaskAsync()
        {
            try
            {
                if (BackgroundTaskRegistration.AllTasks.Values.Any(i => i.Name.Equals(TOAST_BACKGROUND_TASK_NAME)))
                    return;

                var status = await BackgroundExecutionManager.RequestAccessAsync();
                var builder = new BackgroundTaskBuilder() { Name = TOAST_BACKGROUND_TASK_NAME };
                builder.SetTrigger(new ToastNotificationActionTrigger());

                var registration = builder.Register();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private async Task StartBackgroundTaskAsync()
        {
            await RegisterBackgroundTaskAsync();
         
            try
            {
                if (ApiInformation.IsApiContractPresent(typeof(StartupTaskContract).FullName, 1))
                {
                    var notifyTask = await StartupTask.GetAsync("UnicordBackgroundTask");
                    await notifyTask.RequestEnableAsync();
                }

                if (ApiInformation.IsApiContractPresent(typeof(FullTrustAppContract).FullName, 1))
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
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

        private void Navigation_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled)
                return;

            if (SettingsOverlayGrid.Visibility == Visibility.Visible)
            {
                CloseSettings();
                e.Handled = true;
            }
        }

        private async Task Notification_MessageCreated(MessageCreateEventArgs e)
        {
            if (!WindowManager.VisibleChannels.Contains(e.Channel.Id))
            {
                if (Tools.WillShowToast(e.Message))
                {
                    if (IsWindowVisible)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShowNotification(e.Message));
                    }
                }
            }
        }

        private void ShowNotification(DiscordMessage message)
        {
            if (MainFrame.CurrentSourcePageType == typeof(ChannelPage))
            {
                notification.Margin = new Thickness(0, 42, 4, 0);
            }
            else
            {
                notification.Margin = new Thickness(0, 20, 4, 0);
            }

            notification.Show(new MessageControl() { Message = message, Margin = new Thickness(-6, -10, -8, 0) }, 7_000);
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
            if (ActualWidth <= 768 || ContentTransform.X < 0)
            {
                _helper.Cancel();
                ClosePaneMobileStoryboard.Begin();
            }
        }

        private async void Notification_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var message = (((InAppNotification)sender).Content as MessageControl)?.Message;
            if (message != null)
            {
                var service = DiscordNavigationService.GetForCurrentView();
                await service.NavigateAsync(message.Channel);
            }
        }

        private async void GuildsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model.Navigating)
                return;

            if (e.AddedItems.FirstOrDefault() is DiscordGuild g)
            {
                if (!g.IsUnavailable)
                {
                    sidebarFrame.Navigate(typeof(GuildChannelListPage), g);
                    Model.IsFriendsSelected = false;
                }
                else
                {
                    Model.SelectedGuild = null;
                    var loader = ResourceLoader.GetForViewIndependentUse();
                    await UIUtilities.ShowErrorDialogAsync(loader.GetString("ServerUnavailableTitle"), loader.GetString("ServerUnavailableMessage"));
                }
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
            var enumerable = ((DiscordPageModel)DataContext).Guilds.Select(g => g.Id).ToArray();
            if (!enumerable.SequenceEqual(App.Discord.UserSettings.GuildPositions))
            {
                await App.Discord.UpdateUserSettingsAsync(enumerable);
            }
        }

        private void CheckSettingsPane()
        {
            if (ActualWidth <= 768)
            {
                SettingsPaneTransform.Y = 0;
                SettingsPaneTransform.X = 0;
                SettingsContainer.Width = double.NaN;
                SettingsContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else
            {
                SettingsPaneTransform.Y = 0;
                SettingsContainer.Width = 450;
                SettingsPaneTransform.X = 450;
                SettingsContainer.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }

        private async void SettingsItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var service = SettingsService.GetForCurrentView();
            await service.OpenAsync();
        }

        private void SettingsOverlayBackground_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CloseSettings();
        }

        internal void OpenSettings(SettingsPageType page)
        {
            Analytics.TrackEvent("DiscordPage_OpenSettings");

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

            SettingsGrid.Navigate(typeof(SettingsPage), page, new SuppressNavigationTransitionInfo());
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
            SettingsGrid.Navigate(typeof(Page), null, new SuppressNavigationTransitionInfo());
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
            _helper.IsEnabled = e.NewSize.Width <= 768;
        }

        private void ClydeLogo_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }
    }
}