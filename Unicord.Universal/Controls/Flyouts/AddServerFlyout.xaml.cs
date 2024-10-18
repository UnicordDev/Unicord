using System;
using Unicord.Universal.Dialogs;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            // this.FindParent<Flyout>()?.Hide();

            var dialog = new CreateServerDialog();
            await dialog.ShowAsync();
        }
    }
}
