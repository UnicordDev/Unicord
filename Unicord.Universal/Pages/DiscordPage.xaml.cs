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
using Unicord.Universal.Models;
using Unicord.Universal.Pages.Settings;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Utilities;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages
{
    public sealed partial class DiscordPage : Page
    {
        public new Frame Frame => mainFrame;
        private ObservableCollection<DiscordGuild> _guilds = new ObservableCollection<DiscordGuild>();
        private ObservableCollection<DiscordDmChannel> _unreadDms = new ObservableCollection<DiscordDmChannel>();
        private MainPageViewModel _args;
        private bool _loaded;
        private bool _visibility;

        private bool _isPaneOpen => ContentTransform.X != 0;

        public DiscordPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            _visibility = Window.Current.Visible;

            Window.Current.VisibilityChanged += Current_VisibilityChanged;

            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.KeyboardAccelerator"))
            {
                //this.AddAccelerator(VirtualKey.Up, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, MoveServerUp_Invoked);
                //this.AddAccelerator(VirtualKey.Down, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, MoveServerDown_Invoked);
            }
        }

        private void MoveServerUp_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            guildsList.SelectedIndex = Math.Max(0, Math.Min(guildsList.SelectedIndex - 1, _guilds.Count));
        }

        private void MoveServerDown_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            guildsList.SelectedIndex = Math.Max(0, Math.Min(guildsList.SelectedIndex + 1, _guilds.Count));
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            _visibility = e.Visible;
            UpdateTitleBar();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateTitleBar();

            if (e.Parameter is MainPageViewModel args)
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
                App.Discord.MessageCreated += Notification_MessageCreated;
                App.Discord.UserSettingsUpdated += Discord_UserSettingsUpdated;
                App.Discord.GuildCreated += Discord_GuildCreated;
                App.Discord.GuildDeleted += Discord_GuildDeleted;
                App.Discord.DmChannelCreated += Discord_DmChannelCreated;
                App.Discord.DmChannelDeleted += Discord_DmChannelDeleted;

                UpdateTitleBar();
                CheckSettingsPane();

                _loaded = true;

                var guildPositions = App.Discord.UserSettings?.GuildPositions;
                foreach (var guild in App.Discord.Guilds.Values.OrderBy(g => guildPositions?.IndexOf(g.Id) ?? 0))
                {
                    _guilds.Add(guild);
                }

                _unreadDms.CollectionChanged += (o, ev) =>
                {
                    if (_unreadDms.Count > 0)
                    {
                        unreadDms.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        unreadDms.Visibility = Visibility.Collapsed;
                    }
                };

                foreach (var dm in App.Discord.PrivateChannels.Values)
                {
                    if (dm.ReadState.MentionCount > 0)
                    {
                        _unreadDms.Add(dm);
                    }

                    dm.PropertyChanged += Dm_PropertyChanged;
                }

                unreadDms.ItemsSource = _unreadDms;
                unreadDms.Visibility = _unreadDms.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                this.FindParent<MainPage>().HideConnectingOverlay();

                if (App.ThemeLoadException != null)
                {
                    await UIUtilities.ShowErrorDialogAsync("Theme failed to load!", $"Your selected theme failed to load. {App.ThemeLoadException.Message}");
                }

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
            catch (Exception ex)
            {
                await UIUtilities.ShowErrorDialogAsync("An error has occured.", ex.Message);
            }
        }

        private async Task Discord_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            await Dispatcher.RunIdleAsync(d => e.Channel.PropertyChanged += Dm_PropertyChanged);
        }

        private async Task Discord_DmChannelDeleted(DmChannelDeleteEventArgs e)
        {
            await Dispatcher.RunIdleAsync(d => e.Channel.PropertyChanged -= Dm_PropertyChanged);
        }

        private async Task Discord_GuildCreated(GuildCreateEventArgs e)
        {
            if (!_guilds.Contains(e.Guild))
            {
                await Dispatcher.RunIdleAsync(d => _guilds.Add(e.Guild));
            }
        }

        private async Task Discord_GuildDeleted(GuildDeleteEventArgs e)
        {
            await Dispatcher.RunIdleAsync(d => _guilds.Remove(e.Guild));
        }

        private void Dm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var d = sender as DiscordDmChannel;
            if (e.PropertyName == nameof(d.ReadState))
            {
                if (d.ReadState.MentionCount > 0)
                {
                    if (!_unreadDms.Contains(d))
                    {
                        _unreadDms.Add(d);
                    }
                }
                else
                {
                    _unreadDms.Remove(d);
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
                App.Discord.MessageCreated -= Notification_MessageCreated;
                App.Discord.UserSettingsUpdated -= Discord_UserSettingsUpdated;
                App.Discord.GuildCreated -= Discord_GuildCreated;
                App.Discord.GuildDeleted -= Discord_GuildDeleted;
                App.Discord.DmChannelCreated -= Discord_DmChannelCreated;
                App.Discord.DmChannelDeleted -= Discord_DmChannelDeleted;

                foreach (var dm in App.Discord.PrivateChannels.Values)
                {
                    dm.PropertyChanged -= Dm_PropertyChanged;
                }
            }
        }

        private async Task Notification_MessageCreated(MessageCreateEventArgs e)
        {
            if (App._currentChannelId != e.Channel.Id)
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
            if (Frame.CurrentSourcePageType == typeof(ChannelPage))
            {
                notification.Margin = new Thickness(0, 42, 4, 0);
            }
            else
            {
                notification.Margin = new Thickness(0, 20, 4, 0);
            }

            notification.Content = new MessageViewer() { Message = message, IsEnabled = false, Background = new SolidColorBrush(Colors.Transparent) };
            notification.Show(7_000);
        }

        private void Notification_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var message = ((sender as InAppNotification).Content as MessageViewer)?.Message;
            if (message != null)
            {
                Navigate(message.Channel, new DrillInNavigationTransitionInfo());
            }
        }

        private async void GuildsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is DiscordGuild g)
            {
                if (!g.IsUnavailable)
                {
                    sidebarFrame.Navigate(typeof(GuildChannelListPage), g);
                    friendsItem.IsSelected = false;
                }
                else
                {
                    guildsList.SelectedItem = null;
                    await UIUtilities.ShowErrorDialogAsync("This server is unavailable!", "It seems this having some problems, and is currently unavailable, sorry!");
                }
            }
        }

        private void UnreadDms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is DiscordDmChannel c)
            {
                Navigate(c);
            }
        }

        internal async void Navigate(DiscordChannel channel, NavigationTransitionInfo info = null)
        {
            try
            {
                CloseSplitPane();

                if (channel is DiscordDmChannel dm && !(sidebarFrame.Content is DMChannelsPage))
                {
                    guildsList.SelectedIndex = -1;
                    unreadDms.SelectedItem = dm;
                    friendsItem.IsSelected = true;
                    sidebarFrame.Navigate(typeof(DMChannelsPage), channel, new DrillInNavigationTransitionInfo());
                }
                else if (channel.Guild != null && (!(sidebarFrame.Content is GuildChannelListPage p) || p.Guild != channel.Guild))
                {
                    friendsItem.IsSelected = false;
                    unreadDms.SelectedIndex = -1;
                    guildsList.SelectedItem = channel.Guild;
                    sidebarFrame.Navigate(typeof(GuildChannelListPage), channel.Guild, new DrillInNavigationTransitionInfo());
                }

                if (channel.Type == ChannelType.Voice)
                {
                    var voice = await VoiceViewModel.StartNewAsync(channel);
                    return;
                }

                if (channel.IsNSFW)
                {
                    if (await WindowsHelloManager.VerifyAsync(Constants.VERIFY_NSFW, "Verify your identity to access this channel"))
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

                if (_args?.IsUriActivation == true)
                {
                    notification.Content = new UriActivationMessage();
                    notification.Show(7_000);
                }

                unreadDms.SelectionChanged -= UnreadDms_SelectionChanged;
                guildsList.SelectionChanged -= GuildsList_SelectionChanged;
            }
            finally
            {
                unreadDms.SelectionChanged += UnreadDms_SelectionChanged;
                guildsList.SelectionChanged += GuildsList_SelectionChanged;
            }

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
            if (await WindowsHelloManager.VerifyAsync(Constants.VERIFY_SETTINGS, "Verify your identity to open settings."))
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

        public void OpenCustomPane(Type pageType, object parameter)
        {
            CustomGrid.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());

            if (ActualWidth > 768)
            {
                CustomContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                OpenCustomStoryboard.Begin();
            }
            else
            {
                CustomContainer.RenderTransformOrigin = new Point(0, 0);
                CustomOpenAnimation.From = ActualWidth;
                CustomOpenAnimation2.To = -ActualWidth;
                OpenCustomMobileStoryboard.Begin();
            }

            CustomOverlayGrid.Visibility = Visibility.Visible;
        }

        public void CloseCustomPane()
        {
            if (ActualWidth > 768)
            {
                CustomContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                CloseCustomStoryboard.Begin();
            }
            else
            {
                CustomContainer.RenderTransformOrigin = new Point(0, 0);
                CustomCloseAnimation.To = ActualWidth;
                CloseCustomMobileStoryboard.Begin();
            }
        }

        private void CloseCustomMobileStoryboard_Completed(object sender, object e)
        {
            CustomOverlayGrid.Visibility = Visibility.Collapsed;
        }

        private void OpenSettingsStoryboard_Completed(object sender, object e)
        {

        }
    }
}
