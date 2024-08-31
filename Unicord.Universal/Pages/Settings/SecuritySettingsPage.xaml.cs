using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.AppCenter;
using Unicord.Universal.Models;
using Unicord.Universal.Parsers.Markdown;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials.UI;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class SecuritySettingsPage : Page
    {
        public SecuritySettingsPage()
        {
            InitializeComponent();
            DataContext = new SecuritySettingsModel();
            MarkdownDocument.KnownSchemes.Add("ms-settings");
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var available = await UserConsentVerifier.CheckAvailabilityAsync();
            if (available != UserConsentVerifierAvailability.Available)
            {
                unavailableText.Visibility = Visibility.Visible;
                settingsContent.IsEnabled = false;
            }
            else
            {
                unavailableText.Visibility = Visibility.Collapsed;
                settingsContent.IsEnabled = true;
            }
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            await AppCenter.SetEnabledAsync((sender as ToggleSwitch).IsOn);
        }

        private async void unavailableText_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(e.Link));
        }
    }
}
