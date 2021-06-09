using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Dialogs;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class AddServerFlyout : UserControl
    {
        public AddServerFlyout()
        {
            InitializeComponent();
        }

        private async void CreateServerButton_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<Flyout>()?.Hide();

            var dialog = new CreateServerDialog();
            await dialog.ShowAsync();
        }
    }
}
