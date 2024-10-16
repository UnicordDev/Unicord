using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Dialogs
{
    public sealed partial class TokenDialog : ContentDialog
    {
        public string Token =>
            TokenTextBox.Password;

        public TokenDialog()
        {
            this.InitializeComponent();
        }

        private async void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".txt");
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                TokenTextBox.Password = await FileIO.ReadTextAsync(file);
            }
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            TokenTextBox.Password = "";
        }

        private void SubtitleTextBlock_LinkClicked(object sender, Controls.LinkClickedEventArgs e)
        {

        }
    }
}
