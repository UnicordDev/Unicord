using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Phone.Media.Devices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls
{
    public sealed partial class EndpointSelectionMenu : UserControl
    {
        public EndpointSelectionMenu()
        {
            this.InitializeComponent();

            if (!ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1))
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            var manager = AudioRoutingManager.GetDefault();
            var current = manager.GetAudioEndpoint();
            var available = manager.AvailableAudioEndpoints;

            var flyout = sender as MenuFlyout;

            CheckEndpoint(flyout, current, available, AvailableAudioRoutingEndpoints.Earpiece, AudioRoutingEndpoint.Earpiece);
            CheckEndpoint(flyout, current, available, AvailableAudioRoutingEndpoints.Speakerphone, AudioRoutingEndpoint.Speakerphone);
            CheckEndpoint(flyout, current, available, AvailableAudioRoutingEndpoints.Bluetooth, AudioRoutingEndpoint.Bluetooth);
        }

        private static void CheckEndpoint(MenuFlyout flyout, AudioRoutingEndpoint current, AvailableAudioRoutingEndpoints available, AvailableAudioRoutingEndpoints flag, AudioRoutingEndpoint check)
        {
            var item = flyout.Items.FirstOrDefault(i => i.Name == flag.ToString()) as ToggleMenuFlyoutItem;
            if (!available.HasFlag(flag))
            {
                item.IsEnabled = false;
            }
            else
            {
                item.IsEnabled = true;
                item.IsChecked = current == check;
            }
        }

        private void Earpiece_Click(object sender, RoutedEventArgs e)
        {
            var manager = AudioRoutingManager.GetDefault();
            manager.SetAudioEndpoint(AudioRoutingEndpoint.Earpiece);
        }

        private void Speakerphone_Click(object sender, RoutedEventArgs e)
        {
            var manager = AudioRoutingManager.GetDefault();
            manager.SetAudioEndpoint(AudioRoutingEndpoint.Speakerphone);
        }

        private void Bluetooth_Click(object sender, RoutedEventArgs e)
        {
            var manager = AudioRoutingManager.GetDefault();
            manager.SetAudioEndpoint(AudioRoutingEndpoint.Bluetooth);
        }


    }
}
