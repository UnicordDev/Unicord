using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Voice;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Phone.Media.Devices;
using Windows.UI.Core;
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
    public sealed partial class VoiceConnectionControl : UserControl
    {
        public VoiceConnectionModel ConnectionModel
        {
            get { return (VoiceConnectionModel)GetValue(ConnectionModelProperty); }
            set { SetValue(ConnectionModelProperty, value); }
        }

        public static readonly DependencyProperty ConnectionModelProperty =
            DependencyProperty.Register("ConnectionModel", typeof(VoiceConnectionModel), typeof(VoiceConnectionControl), new PropertyMetadata(null));

        public VoiceConnectionControl()
        {
            this.InitializeComponent();
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await ConnectionModel?.DisconnectAsync();
        }

        private async void MuteToggleButton_Click(object sender, RoutedEventArgs e)
        {
            await ConnectionModel?.ToggleMuteAsync();
        }

        private async void DeafenToggleButton_Click(object sender, RoutedEventArgs e)
        {
            await ConnectionModel?.ToggleDeafenAsync();
        }

       
    }
}
