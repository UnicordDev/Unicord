using System;
using Microsoft.AppCenter;
using Unicord.Universal.Controls;
using Unicord.Universal.Models;
using Unicord.Universal.Parsers.Markdown;
using Windows.Security.Credentials.UI;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

        private async void unavailableText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(e.Link));
        }
    }
}
