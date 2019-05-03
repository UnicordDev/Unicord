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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            nav.SelectedItem = nav.MenuItems.First();
            frame.Navigate(typeof(AccountsSettingsPage));
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
