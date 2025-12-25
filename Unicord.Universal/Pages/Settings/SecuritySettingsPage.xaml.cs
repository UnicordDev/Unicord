using System;
using Microsoft.AppCenter;
using Unicord.Universal.Models;
using Windows.ApplicationModel.Resources;
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
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var resources = ResourceLoader.GetForCurrentView("SecuritySettingsPage");
            unavailableText.Title = resources.GetString("WindowsHelloUnavailable/Text");
            
            var available = await UserConsentVerifier.CheckAvailabilityAsync();
            if (available != UserConsentVerifierAvailability.Available)
            {
                unavailableText.IsOpen = true;
                settingsContent.IsEnabled = false;
            }
            else
            {
                unavailableText.IsOpen = false;
                settingsContent.IsEnabled = true;
            }
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            await AppCenter.SetEnabledAsync((sender as ToggleSwitch).IsOn);
        }

        private async void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-deviceencryption"));
        }
    }
}
