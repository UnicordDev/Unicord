using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Unicord.Universal.Controls;
using Unicord.Universal.Dialogs;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Settings;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Unicord.Universal.Voice;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class DiscordPage : Page
    {
        public Frame MainFrame => mainFrame;
        public Frame SidebarFrame => sidebarFrame;

        private MainPageArgs _args;
        private bool _loaded;

        internal DiscordPageModel Model { get; }

        private bool _visibility;
        private SwipeOpenHelper _helper;

        private bool _isPaneOpen => ContentTransform.X != 0;

        public DiscordPage()
        {
            InitializeComponent();
            Model = DataContext as DiscordPageModel;

            _visibility = Window.Current.Visible;
            _helper = new SwipeOpenHelper(content, this, OpenPaneMobileStoryboard, ClosePaneMobileStoryboard);

            Window.Current.VisibilityChanged += Current_VisibilityChanged;
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            _visibility = e.Visible;
            UpdateTitleBar();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateTitleBar();

            if (e.Parameter is MainPageArgs args)
            {
                _args = args;

                if (_loaded)
                {
                    var channel = await App.Discord.GetChannelAsync(_args.ChannelId);
                    var service = DiscordNavigationService.GetForCurrentView();
                    await service.NavigateAsync(channel);
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
            try
            {
                App.Discord.MessageCreated += Notification_MessageCreated;
                
                UpdateTitleBar();
                CheckSettingsPane();

                _loaded = true;

                this.FindParent<MainPage>().HideConnectingOverlay();

                var service = DiscordNavigationService.GetForCurrentView();

                if (_args != null)
                {
                    var channel = await App.Discord.GetChannelAsync(_args.ChannelId);
                    await service.NavigateAsync(channel);
                }
                else
                {
                    Model.IsFriendsSelected = true;
                    SidebarFrame.Navigate(typeof(DMChannelsPage));
                    MainFrame.Navigate(typeof(FriendsPage));
                }

                var possibleConnection = await VoiceConnectionModel.FindExistingConnectionAsync();
                if (possibleConnection != null)
                {
                    (DataContext as DiscordPageModel).VoiceModel = possibleConnection;
                }

                //if (App.ThemeLoadException != null)
                //{
                //    await UIUtilities.ShowErrorDialogAsync("Theme failed to load!", $"Your selected theme failed to load. {App.ThemeLoadException.Message}");
                //}

                await ContactListManager.UpdateContactsListAsync();
            }
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("An error has occured.", ex.Message);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
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
                if (SharedTools.WillShowToast(e.Message))
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
            if (ActualWidth <= 768 || ContentTransform.X < 0)
            {
                ClosePaneMobileStoryboard.Begin();
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

            notification.Content = message;
            notification.Show(7_000);
        }

        private async  void Notification_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var message = ((sender as InAppNotification).Content as MessageViewer)?.Message;
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

        private void friendsItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Model.IsFriendsSelected = true;
            Model.SelectedGuild = null;
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
            var enumerable = (DataContext as DiscordPageModel).Guilds.Select(g => g.Id);
            if (!enumerable.SequenceEqual(App.Discord.UserSettings.GuildPositions))
            {
                await App.Discord.UpdateUserSettingsAsync(enumerable.ToArray());
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
            var loader = ResourceLoader.GetForViewIndependentUse();
            if (await WindowsHelloManager.VerifyAsync(Constants.VERIFY_SETTINGS, loader.GetString("VerifySettingsDisplayReason")))
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

            SettingsGrid.Navigate(typeof(SettingsPage), null, new SuppressNavigationTransitionInfo());
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
            SettingsGrid.Navigate(typeof(Page), null, new SuppressNavigationTransitionInfo());
        }

        private void CloseItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CloseSplitPane();
        }

        private void CreateServerItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
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
    }
}
