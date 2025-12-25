using System;
using System.Collections.Generic;
using System.Linq;
using Unicord.Universal.Services;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Lib = Microsoft.UI.Xaml.Controls;

namespace Unicord.Universal.Pages.Settings
{
    
    public interface INotifyOnExit
    {
        void OnClosing();
    }

    public sealed partial class SettingsPage : Page, IOverlay
    {
        private long _isPaneOpenCallbackToken;
        private bool _isPaneOpening;
        private bool _focusSearchOnNextPaneOpened;

        // these should be kept in order as they appear in the UI,
        // and in sync with Unicord.Universal.Services.SettingsPage
        private static Dictionary<SettingsPageType, Type> _pages
            = new Dictionary<SettingsPageType, Type>()
            {
                [SettingsPageType.Accounts] = typeof(AccountsSettingsPage),
                [SettingsPageType.Messaging] = typeof(MessagingSettingsPage),
                [SettingsPageType.Notifications] = typeof(NotificationsSettingsPage),
                [SettingsPageType.Themes] = typeof(ThemesSettingsPage),
                [SettingsPageType.Media] = typeof(MediaSettingsPage),
                [SettingsPageType.Voice] = typeof(VoiceSettingsPage),
                [SettingsPageType.Security] = typeof(SecuritySettingsPage),
                [SettingsPageType.About] = typeof(AboutSettingsPage),
            };

#if STORE
        public bool IsDebug => false;
#else
        public bool IsDebug => true;
#endif

        public Size PreferredSize { get; }

        public SettingsPage()
        {
            InitializeComponent();
            SelectNavItem(SettingsPageType.Accounts);
            
            // Load user info
            var user = DiscordManager.Discord?.CurrentUser;
            if (user != null)
            {
                UserDisplayName.Text = user.GlobalName ?? user.Username;
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    UserProfilePicture.ProfilePicture = new BitmapImage(new Uri(user.AvatarUrl));
                }
            }

            // Set initial display mode state
            Loaded += (s, e) =>
            {
                if (_isPaneOpenCallbackToken == 0)
                {
                    _isPaneOpenCallbackToken = NavView.RegisterPropertyChangedCallback(
                        Lib.NavigationView.IsPaneOpenProperty,
                        (_, __) => UpdateSearchDisplayMode());
                }

                UpdateSearchDisplayMode();
            };
        }

        private void SelectNavItem(SettingsPageType pageType)
        {
            if (NavView == null)
                return;

            var tag = pageType.ToString();
            var item = NavView.MenuItems
                .Concat(NavView.FooterMenuItems)
                .OfType<Lib.NavigationViewItem>()
                .FirstOrDefault(i => string.Equals(i.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));

            if (item != null)
                NavView.SelectedItem = item;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is SettingsPageType t)
            {
                SelectNavItem(t);
            }

            var manager = SystemNavigationManager.GetForCurrentView();
            manager.BackRequested += OnBackRequested;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            var manager = SystemNavigationManager.GetForCurrentView();
            manager.BackRequested -= OnBackRequested;

            if (_isPaneOpenCallbackToken != 0)
            {
                NavView.UnregisterPropertyChangedCallback(Lib.NavigationView.IsPaneOpenProperty, _isPaneOpenCallbackToken);
                _isPaneOpenCallbackToken = 0;
            }

            MainFrame.Navigate(typeof(Page));
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (MainFrame.Content is INotifyOnExit notify)
                notify.OnClosing();

            OverlayService.GetForCurrentView().CloseOverlay();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void SettingsCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is INotifyOnExit notify)
                notify.OnClosing();

            OverlayService.GetForCurrentView().CloseOverlay();
        }

        private void NavView_BackRequested(Lib.NavigationView sender, Lib.NavigationViewBackRequestedEventArgs args)
        {
            if (MainFrame.Content is INotifyOnExit notify)
                notify.OnClosing();

            OverlayService.GetForCurrentView().CloseOverlay();
        }

        private void NavView_SelectionChanged(Lib.NavigationView sender, Lib.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer.Tag is not string str || 
                !Enum.TryParse<SettingsPageType>(str, out var page) || 
                !_pages.TryGetValue(page, out var type))
                return;

            if (MainFrame.Content is INotifyOnExit notify)
                notify.OnClosing();

            var transitionInfo = args.RecommendedNavigationTransitionInfo;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
            {
                transitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom };
            }

            MainFrame.Navigate(type, transitionInfo);
        }

        private void NavView_DisplayModeChanged(Lib.NavigationView sender, Lib.NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateSearchDisplayMode();
        }

        private void NavView_PaneOpening(Lib.NavigationView sender, object args)
        {
            // Hide compact UI immediately when opening begins.
            // Also request focus for the search box once opening completes.
            if (SearchIconContainer?.Visibility == Visibility.Visible)
                _focusSearchOnNextPaneOpened = true;

            _isPaneOpening = true;
            UpdateSearchDisplayMode();
        }

        private void NavView_PaneOpened(Lib.NavigationView sender, object args)
        {
            _isPaneOpening = false;
            UpdateSearchDisplayMode();

            if (_focusSearchOnNextPaneOpened)
            {
                _focusSearchOnNextPaneOpened = false;
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    SearchBox?.Focus(FocusState.Programmatic);
                });
            }
        }

        private void NavView_PaneClosing(Lib.NavigationView sender, Lib.NavigationViewPaneClosingEventArgs args)
        {
            _isPaneOpening = false;
            UpdateSearchDisplayMode();
        }

        private void UpdateSearchDisplayMode()
        {
            // Note: DisplayMode may still be Expanded when the pane is closed.
            var paneOpen = NavView.IsPaneOpen;
            var paneOpening = _isPaneOpening && !paneOpen;
            var showCompact = !paneOpen && !paneOpening;

            var theme = ThemeService.GetForCurrentView().GetTheme();

            UpdateSpecialRowMargins(paneOpen, paneOpening);

            // Suppress NavigationViewItem hover highlight only when the full search box is visible.
            SetSearchRowHighlightSuppressed(paneOpen);

            if (showCompact)
            {
                // Pane closed: show icons only
                SearchBoxContainer.Visibility = Visibility.Collapsed;
                SearchIconContainer.Visibility = Visibility.Visible;
                ProfileDetails.Visibility = Visibility.Collapsed;
                ProfileItem.Visibility = Visibility.Visible;

                // Keep compact icons aligned with the theme's default nav icons.
                // SunValley already matches; Fluent/Performance need a slight left nudge.
                SearchIconContainer.Margin = theme == AppTheme.SunValley
                    ? new Thickness(0)
                    : new Thickness(-16, 0, 0, 0);
                
                // Compact pane: center avatar inside the full row (avoid large right-side empty space)
                Grid.SetColumnSpan(UserProfilePicture, 2);
                UserProfilePicture.HorizontalAlignment = HorizontalAlignment.Center;
                UserProfilePicture.Width = 24;
                UserProfilePicture.Height = 24;
                UserProfilePicture.Margin = theme == AppTheme.SunValley
                    ? new Thickness(-8, 8, 0, 8)
                    : new Thickness(-16, 8, 0, 8);
            }
            else if (paneOpening)
            {
                // Pane is animating open: hide compact icon immediately so it doesn't linger.
                // The full search box will appear once the pane is actually open.
                SearchBoxContainer.Visibility = Visibility.Collapsed;
                SearchIconContainer.Visibility = Visibility.Collapsed;
                ProfileDetails.Visibility = Visibility.Collapsed;
                ProfileItem.Visibility = Visibility.Visible;

                SearchIconContainer.Margin = theme == AppTheme.SunValley
                    ? new Thickness(0)
                    : new Thickness(-4, 0, 0, 0);
                
                Grid.SetColumnSpan(UserProfilePicture, 2);
                UserProfilePicture.HorizontalAlignment = HorizontalAlignment.Center;
                UserProfilePicture.Width = 24;
                UserProfilePicture.Height = 24;
                UserProfilePicture.Margin = theme == AppTheme.SunValley
                    ? new Thickness(-3, 8, 0, 8)
                    : new Thickness(-4, 8, 0, 8);
            }
            else
            {
                // Pane open: show full UI
                SearchBoxContainer.Visibility = Visibility.Visible;
                SearchIconContainer.Visibility = Visibility.Collapsed;
                ProfileDetails.Visibility = Visibility.Visible;
                ProfileItem.Visibility = Visibility.Visible;

                SearchIconContainer.Margin = new Thickness(0);
                
                // Left align avatar in expanded mode and restore normal size
                Grid.SetColumnSpan(UserProfilePicture, 1);
                UserProfilePicture.HorizontalAlignment = HorizontalAlignment.Left;
                UserProfilePicture.Width = 48;
                UserProfilePicture.Height = 48;
                UserProfilePicture.Margin = new Thickness(5, 8, 10, 8);
            }
        }

        private void UpdateSpecialRowMargins(bool paneOpen, bool paneOpening)
        {
            if (SearchBoxContainer == null || ProfileContainer == null)
                return;

            // Never apply insets in compact/pane-opening states; it would offset the centered icons.
            if (!paneOpen || paneOpening)
            {
                SearchBoxContainer.Margin = new Thickness(0);
                ProfileContainer.Margin = new Thickness(0);
                return;
            }

            var theme = ThemeService.GetForCurrentView().GetTheme();
            switch (theme)
            {
                case AppTheme.SunValley:
                    // SunValley uses different NavigationView paddings; keep the tuned layout.
                    SearchBoxContainer.Margin = new Thickness(0, 0, -6.5, 0);
                    ProfileContainer.Margin = new Thickness(0);
                    break;

                case AppTheme.Fluent:
                case AppTheme.Performance:
                default:
                    // Win10-era NavigationView has more built-in insets; reintroduce them for the special rows.
                    SearchBoxContainer.Margin = new Thickness(-5, 0, 11, 0);
                    ProfileContainer.Margin = new Thickness(-5, 0, 11, 0);
                    break;
            }
        }

        private void SetSearchRowHighlightSuppressed(bool suppress)
        {
            if (SearchItem == null)
                return;

            var keys = new[]
            {
                "NavigationViewItemBackgroundPointerOver",
                "NavigationViewItemBackgroundPressed",
                "NavigationViewItemBackgroundCheckedPointerOver",
                "NavigationViewItemBackgroundCheckedPressed",
                "NavigationViewItemBackgroundSelectedPointerOver",
                "NavigationViewItemBackgroundSelectedPressed",
            };

            if (suppress)
            {
                var transparent = new SolidColorBrush(Colors.Transparent);
                foreach (var key in keys)
                    SearchItem.Resources[key] = transparent;
            }
            else
            {
                foreach (var key in keys)
                {
                    if (SearchItem.Resources.ContainsKey(key))
                        SearchItem.Resources.Remove(key);
                }
            }
        }

        private void ProfileItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Clicking the profile row should not keep the AutoSuggestBox focused/selected.
            if (SearchBox != null)
            {
                SearchBox.IsSuggestionListOpen = false;
            }

            if (sender is Control c)
                c.Focus(FocusState.Pointer);
            else
                NavView?.Focus(FocusState.Pointer);

            e.Handled = true;
        }

        private void SearchItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Only meaningful when pane is closed (compact/icon mode).
            if (NavView?.IsPaneOpen == true || SearchIconContainer?.Visibility != Visibility.Visible)
                return;

            // Immediately show full UI and focus search box
            SearchBoxContainer.Visibility = Visibility.Visible;
            SearchIconContainer.Visibility = Visibility.Collapsed;
            ProfileDetails.Visibility = Visibility.Visible;
            
            // Suppress NavigationViewItem hover highlight immediately
            SetSearchRowHighlightSuppressed(true);
            
            // Force clear any pressed/selected visual state on SearchItem
            SearchItem.IsEnabled = false;
            SearchItem.IsEnabled = true;
            
            SearchBox.Focus(FocusState.Programmatic);
            
            NavView.IsPaneOpen = true;
            e.Handled = true;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchTerm = sender.Text.ToLower();
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    sender.ItemsSource = null;
                    return;
                }

                // Search through navigation items (skip items without tags)
                var results = new List<Lib.NavigationViewItem>();
                foreach (var item in NavView.MenuItems.Concat(NavView.FooterMenuItems).OfType<Lib.NavigationViewItem>())
                {
                    if (item.Tag == null) continue; // Skip profile/search items
                    
                    var content = item.Content?.ToString()?.ToLower() ?? "";
                    if (content.Contains(searchTerm))
                    {
                        results.Add(item);
                    }
                }

                sender.ItemsSource = results.Select(r => r.Content).ToList();
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null || !string.IsNullOrWhiteSpace(args.QueryText))
            {
                var searchText = (args.ChosenSuggestion?.ToString() ?? args.QueryText).ToLower();
                var item = NavView.MenuItems.Concat(NavView.FooterMenuItems)
                    .OfType<Lib.NavigationViewItem>()
                    .Where(i => i.Tag != null) // Skip profile/search items
                    .FirstOrDefault(i => i.Content?.ToString()?.ToLower() == searchText);

                if (item != null)
                {
                    NavView.SelectedItem = item;
                }
            }
        }
    }
}
