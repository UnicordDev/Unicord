﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls
{
    public sealed partial class UploadItemsControl : UserControl
    {
        private enum MediaType
        {
            Audio, Video, Photo
        }

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

                var model = DataContext as ChannelPageViewModel;
                var type = file.ContentType;
                var progress = new Progress<double?>(UpdateProgressBar);
                var setting = (MediaTranscodeOptions)App.RoamingSettings.Read(Constants.AUTO_TRANSCODE_MEDIA, (int)MediaTranscodeOptions.WhenNeeded);
                var props = await file.GetBasicPropertiesAsync();
                _cancellationToken = new CancellationTokenSource();

                var transcodeFailed = false;

                if (!temporary && setting == MediaTranscodeOptions.Always || (setting == MediaTranscodeOptions.WhenNeeded && props.Size >= (ulong)model.UploadLimit))
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
                        model.IsTranscoding = true;
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
                            if (!_cancellationToken.IsCancellationRequested)
                            {
                                transcodeFailed = true;
                            }
                        }
                    }

                    props = await file.GetBasicPropertiesAsync();
                    transcodeProgress.IsIndeterminate = false;
                    transcodeOverlay.Visibility = Visibility.Collapsed;
                }

                var fileModel = await FileUploadModel.FromStorageFileAsync(model, file, props, temporary, transcodeFailed);
                model.FileUploads.Add(fileModel);

                model.IsTranscoding = false;

                _transcodeWait.Release();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
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
                transcodeProgress.IsIndeterminate = false;
                transcodeProgress.Value = obj.Value;
            }
        }

        private async void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as FileUploadModel;
            var channelViewModel = DataContext as ChannelPageViewModel;
            channelViewModel.FileUploads.Remove(item);

            if (item.IsTemporary)
            {
                await item.StorageFile.DeleteAsync();
            }

            item.Dispose();
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
            var channelViewModel = (ChannelPageViewModel)DataContext;

            try
            {
                switch (type)
                {
                    case MediaType.Audio:
                        tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Path.ChangeExtension(file.Name, ".mp3"), CreationCollisionOption.GenerateUniqueName);
                        success = await MediaTranscoding.TryTranscodeAudioAsync(file, tempFile, channelViewModel.HasNitro, progress, token);
                        break;
                    case MediaType.Video:
                        tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Path.ChangeExtension(file.Name, ".mp4"), CreationCollisionOption.GenerateUniqueName);
                        success = await MediaTranscoding.TryTranscodeVideoAsync(file, tempFile, progress, token);
                        break;
                    case MediaType.Photo:
                        tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(file.Name, CreationCollisionOption.GenerateUniqueName);
                        success = await MediaTranscoding.TryTranscodePhotoAsync(file, tempFile, progress, token);
                        break;
                    default:
                        return file;
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException))
                {
                    Logger.LogError(ex);
                }
            }

            if (success)
            {
                return tempFile;
            }

            if (tempFile != null)
                await tempFile.DeleteAsync();

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
