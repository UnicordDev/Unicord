using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Lib = Microsoft.UI.Xaml.Controls;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class SettingsPage : Page
    {
        // these should be kept in order as they appear in the UI,
        // and in sync with Unicord.Universal.Services.SettingsPage
        private static Dictionary<SettingsPageType, Type> _pages = new Dictionary<SettingsPageType, Type>()
        {
            [SettingsPageType.Acccounts] = typeof(AccountsSettingsPage),
            [SettingsPageType.Messaging] = typeof(MessagingSettingsPage),
            [SettingsPageType.Themes] = typeof(ThemesSettingsPage),
            [SettingsPageType.Media] = typeof(MediaSettingsPage),
            [SettingsPageType.Voice] = typeof(VoiceSettingsPage),
            [SettingsPageType.Security] = typeof(SecuritySettingsPage),
#if !STORE
            [SettingsPageType.Debug] = typeof(AboutSettingsPage),
#endif
            [SettingsPageType.About] = typeof(AboutSettingsPage),
        };

#if STORE
        public bool IsDebug => false;
#else
        public bool IsDebug => true;
#endif

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is SettingsPageType t)
            {
                if (_pages.TryGetValue(t, out var type))
                {
                    var newIndex = _pages.Keys.ToList().IndexOf(t);
                    SettingsTabView.SelectedIndex = newIndex;
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
            var page = this.FindParent<DiscordPage>();
            page.CloseSettings();

            e.Handled = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WindowingService.Current.HandleTitleBarForControl(SettingsTabView, true);
        }

        private void SettingsCloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = this.FindParent<DiscordPage>();
            page.CloseSettings();
        }
    }
}
