using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using WamWooWam.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Popups;
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
    public sealed partial class CameraControl : UserControl
    {
        public event EventHandler<StorageFile> FileChosen;

        public CameraControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void popoutButton_Click(object sender, RoutedEventArgs e)
        {
            var cameraUi = new CameraCaptureUI();
            var file = await cameraUi.CaptureFileAsync(CameraCaptureUIMode.PhotoOrVideo);

            if (App.RoamingSettings.Read("SavePhotos", true))
            {
                await file.MoveAsync(KnownFolders.CameraRoll, DateTimeOffset.Now.ToString("yyyy-MM-dd HH-mm-ss") + Path.GetExtension(file.Path));
            }

            if (file != null)
            {
                FileChosen?.Invoke(this, file);
            }
        }

        private async void openLocalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    CommitButtonText = $"Upload",
                    ViewMode = PickerViewMode.Thumbnail
                };

                picker.FileTypeFilter.Add("*");

                var files = await picker.PickMultipleFilesAsync();
                foreach (var file in files)
                {
                    FileChosen?.Invoke(this, file);
                }
            }
            catch { }
        }
    }
}
