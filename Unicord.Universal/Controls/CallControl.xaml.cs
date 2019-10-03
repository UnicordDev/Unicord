using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Unicord.Universal.Models;
using Unicord.Universal.Pages;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class CallControl : UserControl
    {
        private bool _isFullScreen = false;
        private Grid _panel;

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(CallViewModel), typeof(CallControl), new PropertyMetadata(null));

        public static readonly DependencyProperty CallProperty =
            DependencyProperty.Register("Call", typeof(DiscordCall), typeof(CallControl), new PropertyMetadata(null, OnCallChanged));

        public CallViewModel Model
        {
            get { return (CallViewModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public DiscordCall Call
        {
            get { return (DiscordCall)GetValue(CallProperty); }
            set { SetValue(CallProperty, value); }
        }

        private static void OnCallChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CallControl control && e.NewValue is DiscordCall call)
            {
                if(call.Channel != control.Model?.Call.Channel)
                {
                    control.Model?.Dispose();
                    control.Model = null;
                    control.Model = new CallViewModel(call);
                    control.Model.PropertyChanged += control.OnModelPropertyChanged;

                    control.DataContext = control.Model;
                }
            }
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Bindings.Update();
        }

        public CallControl()
        {
            this.InitializeComponent();
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            var page = this.FindParent<MainPage>();

            if (!_isFullScreen)
            {
                _panel = this.Parent as Grid;
                page.EnterFullscreen(this, _panel);
                (sender as Button).Content = "\xE73F";
            }
            else
            {
                page.LeaveFullscreen(this, _panel);
                (sender as Button).Content = "\xE740";
            }

            _isFullScreen = !_isFullScreen;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Model.Dispose();
            Model = null;
        }

        private async void JoinCallButton_Click(object sender, RoutedEventArgs e)
        {
            await Model.Connection?.ConnectAsync();
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await Model.Connection?.DisconnectAsync();
        }      
        
        private async void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            await Model.Connection?.ToggleMuteAsync();
        }

        private async void DeafenButton_Click(object sender, RoutedEventArgs e)
        {
            await Model.Connection?.ToggleDeafenAsync();
        }
    }
}
