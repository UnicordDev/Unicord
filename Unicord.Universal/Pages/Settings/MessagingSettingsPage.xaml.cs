using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class MessagingSettingsPage : Page
    {
        public MessagingSettingsPage()
        {
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // dirty hack to work around databinding fuckery
            App.RoamingSettings.Save(Constants.TIMESTAMP_STYLE, (sender as ComboBox).SelectedIndex);
        }

        private async void ContrastLearnMoreButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://www.w3.org/TR/WCAG/#contrast-minimum"));
        }
    }
}
