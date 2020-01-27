using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Controls;
using Unicord.Universal.Controls.Messages;
using Unicord.Universal.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
            App.RoamingSettings.Save("TimestampStyle", (TimestampStyle)(sender as ComboBox).SelectedIndex);
            ((DataContext as MessagingSettingsModel).ExampleMessage as MockMessage).NotifyAllChanged();
        }
    }
}
