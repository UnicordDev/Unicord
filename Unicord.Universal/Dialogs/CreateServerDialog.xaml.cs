using System;
using System.IO;
using Unicord.Universal.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Unicord.Universal.Services;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Dialogs
{
    public sealed partial class CreateServerDialog : ContentDialog
    {
        public CreateServerDialog()
        {
            InitializeComponent();
        }

        private async void ChooseIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DataContext is CreateServerModel model)
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".gif");
                picker.FileTypeFilter.Add(".webp");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    IconProgressRing.IsActive = true;

                    model.IconFile = file;
                    if (!(model.Icon is BitmapImage image))
                    {
                        image = new BitmapImage
                        {
                            DecodePixelHeight = 128,
                            DecodePixelType = DecodePixelType.Logical
                        };

                        model.Icon = image;
                    }

                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        await image.SetSourceAsync(stream);
                    }

                    IconProgressRing.IsActive = false;
                }
            }
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            return;

            if (DataContext is CreateServerModel model)
            {
                if (string.IsNullOrEmpty(model.Name))
                {
                    args.Cancel = true;
                    return;
                }

                if (model.Region == null) // should never happen, better safe than sorry.
                {
                    args.Cancel = true;
                    return;
                }

                try
                {
                    if (model.IconFile != null)
                    {
                        using (var stream = await model.IconFile.OpenStreamForReadAsync())
                        {
                            await DiscordManager.Discord.CreateGuildAsync(model.Name, model.Region.Id, stream);
                        }
                    }
                    else
                    {
                        await DiscordManager.Discord.CreateGuildAsync(model.Name, model.Region.Id);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }
    }
}
