using System.Linq;
using DSharpPlus.Entities;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.User;
using Unicord.Universal.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Lib = Microsoft.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ProfileOverlay : UserControl
    {
        private const double CompactThresholdWidth = 640;
        private string _pendingNavigationTag;

        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(UserViewModel), typeof(ProfileOverlay), new PropertyMetadata(null, OnUserChanged));

        private static void OnUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlay = (ProfileOverlay)d;
            overlay.Bindings.Update();

            overlay.ApplyPendingNavigationOrDefault();
        }

        public ProfileOverlay()
        {
            InitializeComponent();

            SizeChanged += (_, __) => UpdatePaneMode();
            
            Loaded += (s, e) =>
            {
                ApplyPendingNavigationOrDefault();

                UpdatePaneMode();
            };
        }

        public void NavigateToSection(string tag)
        {
            _pendingNavigationTag = tag;
            ApplyPendingNavigationOrDefault();
        }

        private void ApplyPendingNavigationOrDefault()
        {
            if (NavView == null)
                return;

            var tag = _pendingNavigationTag;
            
            Lib.NavigationViewItem targetItem = tag switch
            {
                "mutualfriends" => MutualFriendsItem,
                "mutual" => MutualServersItem,
                "activities" => ActivitiesItem,
                _ => null
            };

            // If we have a pending navigation target, apply it
            if (targetItem != null)
            {
                _pendingNavigationTag = null;
                NavView.SelectedItem = targetItem;
            }
            // Only apply default Overview if nothing is selected yet
            else if (NavView.SelectedItem == null && _pendingNavigationTag == null)
            {
                NavView.SelectedItem = OverviewItem;
            }
        }

        private void UpdatePaneMode()
        {
            if (NavView == null)
                return;

            var isCompact = ActualWidth > 0 && ActualWidth < CompactThresholdWidth;

            var desiredMode = isCompact
                ? Lib.NavigationViewPaneDisplayMode.LeftCompact
                : Lib.NavigationViewPaneDisplayMode.Left;

            if (NavView.PaneDisplayMode != desiredMode)
                NavView.PaneDisplayMode = desiredMode;

            var desiredIsPaneOpen = !isCompact;
            if (NavView.IsPaneOpen != desiredIsPaneOpen)
                NavView.IsPaneOpen = desiredIsPaneOpen;

            NavView.InvalidateMeasure();
            NavView.UpdateLayout();
        }

        private void NavView_SelectionChanged(Lib.NavigationView sender, Lib.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer?.Tag is not string tag)
                return;

            // Hide all content
            OverviewContent.Visibility = Visibility.Collapsed;
            ActivitiesContent.Visibility = Visibility.Collapsed;
            MutualFriendsContent.Visibility = Visibility.Collapsed;
            MutualServersContent.Visibility = Visibility.Collapsed;

            // Show selected content
            switch (tag)
            {
                case "overview":
                    OverviewContent.Visibility = Visibility.Visible;
                    break;
                case "activities":
                    ActivitiesContent.Visibility = Visibility.Visible;
                    break;
                case "mutualfriends":
                    MutualFriendsContent.Visibility = Visibility.Visible;
                    break;
                case "mutual":
                    MutualServersContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void DropShadowPanel_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                OverlayService.GetForCurrentView()
                    .CloseOverlay();
            }
        }

        private async void MutualServersList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Unicord.Universal.Models.Guild.GuildViewModel guild)
            {
                OverlayService.GetForCurrentView().CloseOverlay();
                
                if (App.LocalSettings.Read(Constants.ENABLE_GUILD_BROWSING, Constants.ENABLE_GUILD_BROWSING_DEFAULT))
                {
                    // Navigate to guild browsing view
                    var discordPage = Window.Current.Content.FindChild<Pages.DiscordPage>();
                    if (discordPage != null)
                    {
                        discordPage.LeftSidebarFrame.Navigate(typeof(Pages.Subpages.GuildChannelListPage), guild.Guild);
                    }
                }
                else
                {
                    // Navigate to previously selected channel or first accessible text channel
                    var channelId = App.RoamingSettings.Read($"GuildPreviousChannels::{guild.Guild.Id}", 0UL);
                    if (!guild.Guild.Channels.TryGetValue(channelId, out var channel) || (!channel.IsAccessible() || !channel.IsText()))
                    {
                        channel = guild.Guild.Channels.Values
                            .Where(c => c.IsAccessible())
                            .Where(c => c.IsText())
                            .OrderBy(c => c.Position)
                            .FirstOrDefault();
                    }

                    if (channel != null)
                    {
                        await DiscordNavigationService.GetForCurrentView()
                            .NavigateAsync(channel);
                    }
                }
            }
        }

        private async void MutualFriendsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Unicord.Universal.Models.User.UserViewModel friendUser)
            {
                await OverlayService.GetForCurrentView()
                    .ReplaceOverlayWithAnimationAsync<Pages.Overlay.UserInfoOverlayPage>(friendUser);
            }
        }

        private void CopyUserId_Click(object sender, RoutedEventArgs e)
        {
            if (User == null)
                return;

            var dataPackage = new DataPackage();
            dataPackage.SetText(User.Id.ToString());
            Clipboard.SetContent(dataPackage);
        }
    }
}
