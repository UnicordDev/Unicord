using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            switch (args.InvokedItemContainer.Tag as string)
            {
                case "Home":
                    frame.Navigate(typeof(AccountsSettingsPage));
                    break;

                case "Messaging":
                    frame.Navigate(typeof(MessagingSettingsPage));
                    break;

                case "Media":
                    frame.Navigate(typeof(MediaSettingsPage));
                    break;

                case "Themes":
                    frame.Navigate(typeof(ThemesSettingsPage));
                    break;

                case "Security":
                    frame.Navigate(typeof(SecuritySettingsPage));
                    break;

                case "About":
                    frame.Navigate(typeof(AboutSettingsPage));
                    break;

                default:
                    break;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Margin = App.StatusBarFill;
            }
        }
    }
}
