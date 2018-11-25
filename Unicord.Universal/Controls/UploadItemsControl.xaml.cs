using Microsoft.HockeyApp;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Abstractions;
using Unicord.Universal.Models;
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
            Audio, Video
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
                var setting = App.RoamingSettings.Read<MediaTranscodeOptions>("AutoTranscodeMedia");
                var props = await file.GetBasicPropertiesAsync();
                _cancellationToken = new CancellationTokenSource();

                if (setting == MediaTranscodeOptions.Always || (setting == MediaTranscodeOptions.WhenNeeded && props.Size >= (ulong)model.UploadLimit))
                {
                    if (type.StartsWith("audio"))
                    {
                        transcodeProgress.IsIndeterminate = true;
                        transcodeOverlay.Visibility = Visibility.Visible;

                        var newFile = await TryTranscodeMediaAsync(file, MediaType.Audio, progress, _cancellationToken.Token);
                        if (newFile != null)
                        {
                            temporary = true;
                            file = newFile;
                        }
                    }
                    if (type.StartsWith("video"))
                    {
                        transcodeProgress.IsIndeterminate = true;
                        transcodeOverlay.Visibility = Visibility.Visible;

                        var newFile = await TryTranscodeMediaAsync(file, MediaType.Video, progress, _cancellationToken.Token);
                        if(newFile != null)
                        {
                            temporary = true;
                            file = newFile;
                        }
                    }
                    
                    props = await file.GetBasicPropertiesAsync();

                    transcodeProgress.IsIndeterminate = false;
                    transcodeOverlay.Visibility = Visibility.Collapsed;
                }

                var fileModel = await FileUploadModel.FromStorageFileAsync(file, props, temporary);
                model.FileUploads.Add(fileModel);

                _transcodeWait.Release();
            }
            catch { }
        }

        private void UpdateProgressBar(double? obj)
        {
            if (obj == null && !transcodeProgress.IsIndeterminate)
            {
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
            var channelViewModel = (DataContext as ChannelViewModel);
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
                    Height = 150;
                    DockPanel.SetDock(uploadSizeContainer, Dock.Right);
                    uploadSizeBar.Margin = new Thickness(-57, 57, -57, 57);
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

                using (var str = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var media = MediaAbstractions.Current as UwpMediaAbstractions;

                    if (type == MediaType.Audio)
                    {
                        success = await media.TryTranscodeAudioAsync(file, str.AsStream(), channelViewModel.HasNitro, progress, token);
                    }

                    if (type == MediaType.Video)
                    {
                        success = await media.TryTranscodeVideoAsync(file, str.AsStream(), channelViewModel.HasNitro, progress, token);
                    }
                }
            }
            catch (Exception ex)
            {
                if(!(ex is TaskCanceledException))
                {
                    HockeyClient.Current.TrackException(ex, new Dictionary<string, string> { ["type"] = "UploadFailure" });
                    await UIAbstractions.Current.ShowFailureDialogAsync(
                        "Failed to upload.",
                        "Failed to upload.",
                        "Whoops, that file didn't transcode nicely, sorry!");
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
    }
}
