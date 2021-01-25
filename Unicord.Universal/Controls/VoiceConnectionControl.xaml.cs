using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Services;
using Unicord.Universal.Voice;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            get => (VoiceConnectionModel)GetValue(ConnectionModelProperty);
            set => SetValue(ConnectionModelProperty, value);
        }

        public static readonly DependencyProperty ConnectionModelProperty =
            DependencyProperty.Register("ConnectionModel", typeof(VoiceConnectionModel), typeof(VoiceConnectionControl), new PropertyMetadata(null, OnConnectionModelChanged));

        public VoiceConnectionControl()
        {
            this.InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        private static void OnConnectionModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VoiceConnectionControl control)
            {
                if (e.NewValue == null)
                {
                    control.Visibility = Visibility.Collapsed;
                    if(e.OldValue is VoiceConnectionModel old)
                    {
                        old.Disconnected -= control.Model_Disconnected;
                    }
                }
                else if (e.NewValue is VoiceConnectionModel model)
                {
                    control.Visibility = Visibility.Visible;
                    model.Disconnected += control.Model_Disconnected;
                }
            }
        }

        private async void Model_Disconnected(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Visibility = Visibility.Collapsed);
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await ConnectionModel?.DisconnectAsync();
        }

        private async void VoiceSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var service = SettingsService.GetForCurrentView();
            await service.OpenAsync(SettingsPageType.Voice);
        }
    }
}
