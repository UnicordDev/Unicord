using Microsoft.HockeyApp;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
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
    public sealed partial class UploadItemsControl : UserControl
    {
        private enum MediaType
        {
            Audio, Video, Photo
        }

        private bool _small = true;
        private CancellationTokenSource _cancellationToken;
        private SemaphoreSlim _transcodeWait = new SemaphoreSlim(1, 1);

        public UploadItemsControl()
        {
            InitializeComponent();
        }

        public async Task AddStorageFileAsync(IStorageFile file, bool temporary = false)
        {
            try
            {
                await _transcodeWait.WaitAsync();

                var model = DataContext as ChannelViewModel;
                var type = file.ContentType;
                var progress = new Progress<double?>(UpdateProgressBar);
                var setting = App.RoamingSettings.Read<MediaTranscodeOptions>(Constants.AUTO_TRANSCODE_MEDIA);
                var props = await file.GetBasicPropertiesAsync();
                _cancellationToken = new CancellationTokenSource();

                var transcodeFailed = false;

                if (setting == MediaTranscodeOptions.Always || (setting == MediaTranscodeOptions.WhenNeeded && props.Size >= (ulong)model.UploadLimit))
                {
                    MediaType? mediaType = null;

                    if (type.StartsWith("audio"))
                    {
                        mediaType = MediaType.Audio;
                    }
                    else if (type.StartsWith("video"))
                    {
                        mediaType = MediaType.Video;
                    }
                    else if (type.StartsWith("image"))
                    {
                        mediaType = MediaType.Photo;
                    }

                    if (mediaType != null)
                    {
                        transcodeProgress.IsIndeterminate = true;
                        transcodeOverlay.Visibility = Visibility.Visible;

                        var newFile = await TryTranscodeMediaAsync(file, mediaType.Value, progress, _cancellationToken.Token);
                        if (newFile != null)
                        {
                            temporary = true;
                            file = newFile;
                        }
                        else
                        {
                            transcodeFailed = true;
                        }
                    }

                    props = await file.GetBasicPropertiesAsync();

                    transcodeProgress.IsIndeterminate = false;
                    transcodeOverlay.Visibility = Visibility.Collapsed;
                }

                var fileModel = await FileUploadModel.FromStorageFileAsync(file, props, temporary, transcodeFailed);
                model.FileUploads.Add(fileModel);

                _transcodeWait.Release();
            }
            catch { }
        }

        private void UpdateProgressBar(double? obj)
        {
            if (obj == null)
            {
                if (!transcodeProgress.IsIndeterminate)
                    transcodeProgress.IsIndeterminate = true;
            }
            else
            {
                transcodeProgress.Value = obj.Value;
            }
        }

        private async void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as FileUploadModel;
            var channelViewModel = DataContext as ChannelViewModel;
            channelViewModel.FileUploads.Remove(item);

            if (item.IsTemporary)
                await item.StorageFile.DeleteAsync();

            item.Dispose();

            if (!channelViewModel.FileUploads.Any())
                Visibility = Visibility.Collapsed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DoSize(ActualWidth);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DoSize(e.NewSize.Width);
        }

        private void DoSize(double width)
        {
            if (width > 640)
            {
                if (_small)
                {
                    Height = 200;
                    DockPanel.SetDock(uploadSizeContainer, Dock.Right);
                    uploadSizeBar.Width = 160;
                    uploadSizeBar.Margin = new Thickness(-87, 87, -87, 87);
                    uploadSizeBarTransform.Angle = -90;
                    sizeRun.FontSize = 28;
                    _small = false;
                }
            }
            else
            {
                if (!_small)
                {
                    Height = 200;
                    uploadSizeBar.Width = double.NaN;
                    DockPanel.SetDock(uploadSizeContainer, Dock.Bottom);
                    uploadSizeBar.Margin = new Thickness(10);
                    uploadSizeBarTransform.Angle = 0;
                    sizeRun.FontSize = 20;
                    _small = true;
                }
            }
        }

        private async void UploadList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FileUploadModel m && m.StorageFile != null)
            {
                await Launcher.LaunchFileAsync(m.StorageFile);
            }
        }

        private async Task<IStorageFile> TryTranscodeMediaAsync(IStorageFile file, MediaType type, IProgress<double?> progress, CancellationToken token)
        {
            var success = false;
            StorageFile tempFile = null;

            try
            {
                var channelViewModel = (DataContext as ChannelViewModel);
                tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(file.Name, CreationCollisionOption.GenerateUniqueName);

                switch (type)
                {
                    case MediaType.Audio:
                        success = await MediaTranscoding.TryTranscodeAudioAsync(file, tempFile, channelViewModel.HasNitro, progress, token);
                        break;
                    case MediaType.Video:
                        success = await MediaTranscoding.TryTranscodeVideoAsync(file, tempFile, channelViewModel.HasNitro, progress, token);
                        break;
                    case MediaType.Photo:
                        success = await MediaTranscoding.TryTranscodePhotoAsync(file, tempFile, progress, token);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException))
                {
                    HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "UploadFailure" });
                }
            }

            if (success)
                return tempFile;
            else
                return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _cancellationToken.Cancel();
        }

        private async void TranscodeFailedButton_Click(object sender, RoutedEventArgs e)
        {
            await UIUtilities.ShowErrorDialogAsync(
                "Failed to transcode!", 
                "This file failed to transcode, it may have been a format I don't understand, or your PC might not have the needed codecs. Sorry!");
        }
    }
}
