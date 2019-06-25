using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Utilities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.System.Profile;
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
        // these should be kept in order as they appear in the UI
        private static Dictionary<string, Type> _pages = new Dictionary<string, Type>()
        {
            ["Home"] = typeof(AccountsSettingsPage),
            ["Messaging"] = typeof(MessagingSettingsPage),
            ["Media"] = typeof(MediaSettingsPage),
            ["Themes"] = typeof(ThemesSettingsPage),
            ["Security"] = typeof(SecuritySettingsPage),
            ["About"] = typeof(AboutSettingsPage),
        };

        public SettingsPage()
        {
            InitializeComponent();
            nav.SelectedItem = nav.MenuItems.First();
            frame.Navigate(typeof(AccountsSettingsPage));
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            frame.Navigate(typeof(Page));
        }

        private void NavigationView_BackRequested(Lib.NavigationView sender, Lib.NavigationViewBackRequestedEventArgs args)
        {
            var page = this.FindParent<DiscordPage>();
            page.CloseSettings();
        }

        private void NavigationView_ItemInvoked(Lib.NavigationView sender, Lib.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer.Tag is string str)
            {
                if (_pages.TryGetValue(str, out var type))
                {
                    var transitionInfo = args.RecommendedNavigationTransitionInfo;
                    var currentIndex = _pages.Values.ToList().IndexOf(frame.CurrentSourcePageType);
                    var newIndex = _pages.Keys.ToList().IndexOf(str);

                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
                    {
                        if (newIndex > currentIndex)
                        {
                            transitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };
                        }
                        else if (newIndex < currentIndex)
                        {
                            transitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft };
                        }
                    }

                    frame.Navigate(type, null, transitionInfo);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WindowManager.HandleTitleBarForControl(nav, true);
        }
    }
}
