using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Dialogs
{
    public sealed partial class UploadFileDialog : ContentDialog
    {
        public string Caption
        {
            get => commentTextBox.Text;
            set => commentTextBox.Text = value;
        }

        public IStorageFile File { get; set; }
        public IRandomAccessStream Stream { get; set; }

        public UploadFileDialog()
        {
            InitializeComponent();
        }

        private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var image = new BitmapImage();

            if (File != null)
            {
                titleTextBox.Text = $"Uploading \"{File.Name}\"";
                await image.SetSourceAsync((await (File as IStorageItemProperties).GetThumbnailAsync(ThumbnailMode.SingleItem)));
            }
            else
            {
                titleTextBox.Text = $"Uploading \"unknown.png\"";
                await image.SetSourceAsync(Stream);
            }

            thumbnailImage.Source = image;
        }
    }
}
