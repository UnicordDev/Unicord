using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserFlyout : AdaptiveFlyout
    {
        private static Thickness MutualIconsMarginForCount(int count)
        {
            var right = count switch
            {
                1 => 18,
                2 => 12,
                3 => 6,
                _ => 12,
            };

            return new Thickness(0, 0, right, 0);
        }

        private bool _rolesExpanded = false;
        private bool _rolesAutoExpanded = false;

        public UserFlyout(object param) : base(param)
        {
            InitializeComponent();
            DataContextChanged += UserFlyout_DataContextChanged;
            Loaded += UserFlyout_Loaded;
        }

        private void UserFlyout_Loaded(object sender, RoutedEventArgs e)
        {
            _rolesAutoExpanded = false;
            UpdateRolesDisplay();
        }

        private void UserFlyout_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateMutualDisplay();
            _rolesExpanded = false;
            _rolesAutoExpanded = false;
            UpdateRolesDisplay();
        }

        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
                return null;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T typed)
                    return typed;

                var result = FindDescendant<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static Panel FindItemsHostPanel(ItemsControl itemsControl, int expectedChildCount)
        {
            if (itemsControl == null)
                return null;

            Panel best = null;

            void Walk(DependencyObject node)
            {
                if (node == null)
                    return;

                if (node is Panel panel)
                {
                    // Prefer the panel that actually hosts the items
                    if (panel.Children?.Count == expectedChildCount)
                    {
                        best = panel;
                        return;
                    }
                }

                var childCount = VisualTreeHelper.GetChildrenCount(node);
                for (var i = 0; i < childCount && best == null; i++)
                    Walk(VisualTreeHelper.GetChild(node, i));
            }

            Walk(itemsControl);
            return best;
        }

        private int GetHiddenRolesCount(double collapsedMaxHeight, int roleCount)
        {
            // Ensure the visual tree is ready
            RolesItemsControl.UpdateLayout();

            var host = FindItemsHostPanel(RolesItemsControl, roleCount);
            if (host == null || host.Children == null || host.Children.Count == 0)
                return Math.Max(1, roleCount - 5);

            var visibleCount = 0;

            foreach (var child in host.Children.OfType<FrameworkElement>())
            {
                if (child.ActualHeight <= 0)
                    continue;

                // Position relative to the host panel
                var topLeft = child.TransformToVisual(host).TransformPoint(new Point(0, 0));
                var bottom = topLeft.Y + child.ActualHeight;

                if (bottom <= collapsedMaxHeight + 0.5)
                    visibleCount++;
            }

            var hidden = roleCount - visibleCount;
            return hidden <= 0 ? 1 : hidden;
        }

        private void UpdateRolesDisplay()
        {
            if (RolesItemsControl == null || RolesExpandButton == null)
                return;

            if (DataContext is not Unicord.Universal.Models.User.UserViewModel user || user.Roles == null)
            {
                RolesExpandButton.Visibility = Visibility.Collapsed;
                RolesItemsControl.MaxHeight = double.PositiveInfinity;
                return;
            }

            const double collapsedMaxHeight = 60; // ~2 rows at approximately 30px per row

            // Determine whether content actually overflows the collapsed height.
            // depending on wrap width and role pill sizes.
            var availableWidth = RolesItemsControl.ActualWidth;
            if (availableWidth <= 0)
                availableWidth = Root?.ActualWidth > 0 ? Root.ActualWidth : 280;

            // Measure full (unclipped) desired height
            var previousMaxHeight = RolesItemsControl.MaxHeight;
            RolesItemsControl.MaxHeight = double.PositiveInfinity;
            RolesItemsControl.Measure(new Windows.Foundation.Size(availableWidth, double.PositiveInfinity));
            var fullHeight = RolesItemsControl.DesiredSize.Height;

            var needsExpander = fullHeight > (collapsedMaxHeight + 0.5);

            if (!needsExpander)
            {
                // Everything fits already; show all and hide expander.
                _rolesExpanded = true;
                _rolesAutoExpanded = true;
                RolesExpandButton.Visibility = Visibility.Collapsed;
                RolesItemsControl.MaxHeight = double.PositiveInfinity;
                return;
            }

            // Many roles (or narrow layout): show expander
            RolesExpandButton.Visibility = Visibility.Visible;
            
            RolesItemsControl.MaxHeight = collapsedMaxHeight;
            RolesExpandIcon.Glyph = "\uE70D"; // ChevronDown
            var hiddenCount = GetHiddenRolesCount(collapsedMaxHeight, user.Roles.Count);
            RolesMoreCount.Text = $"+{hiddenCount} more";
            RolesExpandTooltip.Text = "Show All Roles";
        }

        private void RolesExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (RolesItemsControl == null || RolesExpandButton == null)
                return;

            if (DataContext is not Unicord.Universal.Models.User.UserViewModel user || user.Roles == null)
                return;

            const double collapsedMaxHeight = 60;

            // Toggle between collapsed and expanded
            var isCurrentlyExpanded = RolesItemsControl.MaxHeight == double.PositiveInfinity;
            
            if (isCurrentlyExpanded)
            {
                // Currently expanded, collapse it
                RolesItemsControl.MaxHeight = collapsedMaxHeight;
                AnimateChevron(0); // Rotate to 0 degrees (down)
                var hiddenCount = GetHiddenRolesCount(collapsedMaxHeight, user.Roles.Count);
                RolesMoreCount.Text = $"+{hiddenCount} more";
                RolesExpandTooltip.Text = "Show All Roles";
            }
            else
            {
                // Currently collapsed, expand it
                RolesItemsControl.MaxHeight = double.PositiveInfinity;
                AnimateChevron(-180); // Rotate to -180 degrees (up, clockwise)
                RolesMoreCount.Text = "Show less";
                RolesExpandTooltip.Text = "Collapse Roles";
            }
        }

        private void AnimateChevron(double toAngle)
        {
            if (RolesExpandIconRotation == null)
                return;

            var storyboard = new Windows.UI.Xaml.Media.Animation.Storyboard();
            var animation = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
            {
                To = toAngle,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new Windows.UI.Xaml.Media.Animation.CubicEase { EasingMode = Windows.UI.Xaml.Media.Animation.EasingMode.EaseOut }
            };
            
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(animation, RolesExpandIconRotation);
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animation, "Angle");
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void UpdateMutualDisplay()
        {
            if (DataContext is not Unicord.Universal.Models.User.UserViewModel user)
                return;

            var mutualFriendsCount = user.MutualFriendsCount;
            var mutualFriends = user.MutualFriends;
            var mutualGuilds = user.MutualGuilds;
            var mutualGuildsCount = mutualGuilds?.Count ?? 0;

            // Reset to a clean baseline so spacing doesn't get stuck when switching users.
            MutualFriendsButton.Visibility = Visibility.Collapsed;
            MutualFriendIcons.Visibility = Visibility.Collapsed;
            MutualServerIcons.Visibility = Visibility.Collapsed;
            MutualSeparator.Visibility = Visibility.Collapsed;
            MutualFriendIcons.Margin = MutualIconsMarginForCount(2);
            MutualServerIcons.Margin = MutualIconsMarginForCount(2);
            MutualFriendsGroup.Visibility = Visibility.Collapsed;
            MutualServersGroup.Visibility = Visibility.Collapsed;

            // If mutual friends exist, show them with friend icons and hide server icons
            if (mutualFriendsCount > 0)
            {
                MutualFriendsGroup.Visibility = Visibility.Visible;
                MutualServersGroup.Visibility = Visibility.Visible;
                MutualFriendsButton.Visibility = Visibility.Visible;
                MutualFriendIcons.Visibility = Visibility.Visible;
                MutualServerIcons.Visibility = Visibility.Collapsed;
                MutualSeparator.Visibility = mutualGuildsCount > 0 ? Visibility.Visible : Visibility.Collapsed;

                // Populate up to 3 friend icons
                var friendIconsToShow = System.Math.Min(3, mutualFriendsCount);

                MutualFriendIcons.Margin = MutualIconsMarginForCount(friendIconsToShow);
                
                FriendIcon1.Visibility = friendIconsToShow >= 1 ? Visibility.Visible : Visibility.Collapsed;
                FriendIcon2.Visibility = friendIconsToShow >= 2 ? Visibility.Visible : Visibility.Collapsed;
                FriendIcon3.Visibility = friendIconsToShow >= 3 ? Visibility.Visible : Visibility.Collapsed;

                if (friendIconsToShow >= 1)
                {
                    FriendIcon1.DisplayName = mutualFriends[0].DisplayName;
                    FriendIcon1.ProfilePicture = CreateImageSource(mutualFriends[0].AvatarUrl);
                }
                if (friendIconsToShow >= 2)
                {
                    FriendIcon2.DisplayName = mutualFriends[1].DisplayName;
                    FriendIcon2.ProfilePicture = CreateImageSource(mutualFriends[1].AvatarUrl);
                }
                if (friendIconsToShow >= 3)
                {
                    FriendIcon3.DisplayName = mutualFriends[2].DisplayName;
                    FriendIcon3.ProfilePicture = CreateImageSource(mutualFriends[2].AvatarUrl);
                }
            }
            else if (mutualGuildsCount > 0)
            {
                MutualServersGroup.Visibility = Visibility.Visible;
                // No mutual friends, show server icons (up to 3)
                MutualFriendsButton.Visibility = Visibility.Collapsed;
                MutualFriendIcons.Visibility = Visibility.Collapsed;
                MutualServerIcons.Visibility = Visibility.Visible;
                MutualSeparator.Visibility = Visibility.Collapsed;

                // Populate up to 3 server icons
                var iconsToShow = System.Math.Min(3, mutualGuildsCount);

                MutualServerIcons.Margin = MutualIconsMarginForCount(iconsToShow);
                
                ServerIcon1.Visibility = iconsToShow >= 1 ? Visibility.Visible : Visibility.Collapsed;
                ServerIcon2.Visibility = iconsToShow >= 2 ? Visibility.Visible : Visibility.Collapsed;
                ServerIcon3.Visibility = iconsToShow >= 3 ? Visibility.Visible : Visibility.Collapsed;

                if (iconsToShow >= 1)
                {
                    ServerIcon1.DisplayName = mutualGuilds[0].Name;
                    ServerIcon1.ProfilePicture = mutualGuilds[0].Icon;
                }
                if (iconsToShow >= 2)
                {
                    ServerIcon2.DisplayName = mutualGuilds[1].Name;
                    ServerIcon2.ProfilePicture = mutualGuilds[1].Icon;
                }
                if (iconsToShow >= 3)
                {
                    ServerIcon3.DisplayName = mutualGuilds[2].Name;
                    ServerIcon3.ProfilePicture = mutualGuilds[2].Icon;
                }
            }
        }

        private static ImageSource CreateImageSource(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            return new BitmapImage(uri);
        }

        private void ViewFullProfile_Click(object sender, RoutedEventArgs e)
        {
            // "View Full Profile" runs via the command binding;
            CloseHostFlyout();
        }

        private async void MutualFriends_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is Unicord.Universal.Models.User.UserViewModel user)
            {
                CloseHostFlyout();
                await OverlayService.GetForCurrentView()
                    .ShowOverlayAsync<Pages.Overlay.UserInfoOverlayPage>((user, "mutualfriends"));
            }
        }

        private async void MutualServers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is Unicord.Universal.Models.User.UserViewModel user)
            {
                CloseHostFlyout();
                await OverlayService.GetForCurrentView()
                    .ShowOverlayAsync<Pages.Overlay.UserInfoOverlayPage>((user, "mutual"));
            }
        }

        private void CopyUserId_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is Unicord.Universal.Models.User.UserViewModel user && user.User?.Id != null)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(user.User.Id.ToString());
                Clipboard.SetContent(dataPackage);
            }
        }

        private void MessageTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Windows.UI.Xaml.Controls.TextBox textBox && DataContext is Unicord.Universal.Models.User.UserViewModel user)
            {
                textBox.PlaceholderText = $"Message @{user.Username}";
            }
        }

        private async void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
                return;

            e.Handled = true;

            var isShiftDown = Window.Current?.CoreWindow?.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) == true;
            if (isShiftDown)
            {
                if (MessageTextBox == null)
                    return;

                var cursorPosition = MessageTextBox.SelectionStart;
                var text = MessageTextBox.Text ?? string.Empty;
                var newline = "\r\n";

                MessageTextBox.Text = text.Insert(cursorPosition, newline);
                MessageTextBox.SelectionStart = cursorPosition + newline.Length;
                return;
            }

            await TrySendMessageAsync();

            // If it sent successfully, close.
            // (TrySendMessageAsync handles empty text / missing DM channel.)
            if (string.IsNullOrWhiteSpace(MessageTextBox?.Text))
                CloseHostFlyout();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var sent = await TrySendMessageAsync();
            if (sent)
                CloseHostFlyout();
        }

        private async Task<bool> TrySendMessageAsync()
        {
            try
            {
                var text = MessageTextBox?.Text;
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                if (DataContext is not Unicord.Universal.Models.User.UserViewModel user)
                    return false;

                DiscordDmChannel dmChannel = null;

                // Prefer the member API when available
                if (user.Member != null)
                {
                    await user.Member.SendMessageAsync(text);
                    
                    // Try to get the DM channel that was created
                    var discord = DiscordManager.Discord;
                    dmChannel = discord?.PrivateChannels.Values
                        .FirstOrDefault(c => c.Type == ChannelType.Private && c.Recipients.Count == 1 && c.Recipients[0].Id == user.Id);
                }
                else
                {
                    // No member context (e.g., DM user). We can only send if a DM channel is already cached.
                    var discord = DiscordManager.Discord;
                    if (discord == null)
                        return false;

                    dmChannel = discord.PrivateChannels.Values
                        .FirstOrDefault(c => c.Type == ChannelType.Private && c.Recipients.Count == 1 && c.Recipients[0].Id == user.Id);

                    if (dmChannel == null)
                        return false;

                    await dmChannel.SendMessageAsync(text);
                }

                MessageTextBox.Text = string.Empty;

                // Navigate to the DM conversation
                if (dmChannel != null)
                {
                    var navigationService = DiscordNavigationService.GetForCurrentView();
                    await navigationService.NavigateAsync(dmChannel);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
        }

        private void EmoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmoteFlyout != null && EmoteButton != null)
            {
                if (EmoteButton.IsChecked == true)
                {
                    EmoteFlyout.ShowAt(EmoteButton);
                }
                else
                {
                    EmoteFlyout.Hide();
                }
            }
        }

        private void EmoteFlyout_Opening(object sender, object e)
        {
            if (DataContext is Unicord.Universal.Models.User.UserViewModel user && EmotePicker != null)
            {
                // If user has a guild context, use any channel from that guild
                if (user.Guild != null)
                {
                    var channelVm = user.Guild.AccessibleChannels?.FirstOrDefault();
                    if (channelVm != null)
                    {
                        EmotePicker.Channel = channelVm.Channel;
                    }
                    else
                    {
                        EmotePicker.Channel = null;
                    }
                }
                else
                {
                    EmotePicker.Channel = null;
                }

                // Refresh items now that Channel may have changed
                EmotePicker.Load();
            }
        }

        private void EmoteFlyout_Closed(object sender, object e)
        {
            if (EmoteButton != null)
            {
                EmoteButton.IsChecked = false;
            }
        }

        private void EmotePicker_EmojiPicked(object sender, Models.Emoji.EmojiViewModel e)
        {
            if (MessageTextBox != null)
            {
                var cursorPosition = MessageTextBox.SelectionStart;
                var text = MessageTextBox.Text ?? "";
                
                // Insert emoji at cursor position
                MessageTextBox.Text = text.Insert(cursorPosition, e.ToString());
                
                // Move cursor after the inserted emoji
                MessageTextBox.SelectionStart = cursorPosition + e.ToString().Length;
                
                // Focus back to the textbox
                MessageTextBox.Focus(FocusState.Programmatic);
            }
            
            // Close the flyout and uncheck the button
            EmoteFlyout?.Hide();
            if (EmoteButton != null)
            {
                EmoteButton.IsChecked = false;
            }
        }

        private async void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            CloseHostFlyout();
            await SettingsService.GetForCurrentView().OpenAsync(SettingsPageType.Accounts);
        }
    }
}
