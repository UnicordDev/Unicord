using System;
using System.Collections.Generic;
using System.Linq;
using Unicord.Universal.Services;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Lib = Microsoft.UI.Xaml.Controls;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class SettingsPage : Page, IOverlay
    {
        // these should be kept in order as they appear in the UI,
        // and in sync with Unicord.Universal.Services.SettingsPage
        private static Dictionary<SettingsPageType, Type> _pages
            = new Dictionary<SettingsPageType, Type>()
            {
                [SettingsPageType.Accounts] = typeof(AccountsSettingsPage),
                [SettingsPageType.Messaging] = typeof(MessagingSettingsPage),
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
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is SettingsPageType t)
            {
                if (_pages.TryGetValue(t, out var type))
                {
                    var newIndex = _pages.Keys.ToList().IndexOf(t);
                    NavView.SelectedItem = NavView.MenuItems.Concat(NavView.FooterMenuItems).ElementAt(newIndex);
                }
            }

            var manager = SystemNavigationManager.GetForCurrentView();
            manager.BackRequested += OnBackRequested;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            var manager = SystemNavigationManager.GetForCurrentView();
            manager.BackRequested -= OnBackRequested;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void SettingsCloseButton_Click(object sender, RoutedEventArgs e)
        {
            OverlayService.GetForCurrentView().CloseOverlay();
        }

        private void NavView_BackRequested(Lib.NavigationView sender, Lib.NavigationViewBackRequestedEventArgs args)
        {
            OverlayService.GetForCurrentView().CloseOverlay();
        }

        private void NavView_SelectionChanged(Lib.NavigationView sender, Lib.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer.Tag is string str && Enum.TryParse<SettingsPageType>(str, out var page))
            {
                if (_pages.TryGetValue(page, out var type))
                {
                    var transitionInfo = args.RecommendedNavigationTransitionInfo;
                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
                    {
                        transitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom };
                    }

                    MainFrame.Navigate(type, transitionInfo);
                }
            }
        }
    }
}
