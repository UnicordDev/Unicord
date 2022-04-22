﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Helpers;
//using Unicord.Universal.Native;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace Unicord.Universal.Controls
{
    public sealed partial class AttachmentViewer : UserControl
    {
        private static readonly string[] _mediaExtensions =
            new string[] { ".gifv", ".mp4", ".mov", ".webm", ".wmv", ".avi", ".mkv", ".ogv", ".mp3", ".m4a", ".aac", ".wav", ".wma", ".flac", ".ogg", ".oga", ".opus" };

        private Size? naturalSize = null;
        private bool useNaturalSize = false;

        private DispatcherTimer _timer;
        private StorageFile _shareFile;
        private ResourceLoader _strings;

        public DiscordAttachment Attachment
        {
            get => (DiscordAttachment)GetValue(AttachmentProperty);
            set => SetValue(AttachmentProperty, value);
        }

        public static readonly DependencyProperty AttachmentProperty =
            DependencyProperty.Register("Attachment", typeof(DiscordAttachment), typeof(AttachmentViewer), new PropertyMetadata(null));

        public AttachmentViewer()
        {
            InitializeComponent();
            _strings = ResourceLoader.GetForCurrentView("Controls");
        }

        public AttachmentViewer(DiscordAttachment attachment) : base()
        {
            Attachment = attachment;
            DataContext = attachment;
            HorizontalAlignment = HorizontalAlignment.Left;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Log(Attachment.ContentType);
            if (Path.GetFileNameWithoutExtension(Attachment.Url).StartsWith("SPOILER_") && App.RoamingSettings.Read(Constants.ENABLE_SPOILERS, true))
            {
                spoilerOverlay.Visibility = Visibility.Visible;
            }

            var url = Attachment.Url;
            var fileExtension = Path.GetExtension(url).ToLowerInvariant();

            naturalSize = Attachment.Width != 0 && Attachment.Height != 0 ? new Size(Attachment.Width, Attachment.Height) : null;
            if (_mediaExtensions.Contains(fileExtension))
            {
                useNaturalSize = true;

                var mediaPlayer = new MediaPlayerElement()
                {
                    AreTransportControlsEnabled = true,
                    Source = MediaSource.CreateFromUri(new Uri(Attachment.ProxyUrl)),
                    PosterSource = Attachment.Width != 0 ? new BitmapImage(new Uri(Attachment.ProxyUrl + "?format=jpeg")) : null
                };

                mediaPlayer.TransportControls.Style = (Style)App.Current.Resources["MediaTransportControlsStyle"];
                mediaPlayer.TransportControls.IsCompact = true;

                if (naturalSize == null)
                {
                    mediaPlayer.MediaPlayer.MediaOpened += OnMediaOpened;
                }

                mainGrid.Child = mediaPlayer;
                detailsGrid.Visibility = Visibility.Collapsed;
            }
            else if (fileExtension == ".svg")
            {
                var source = new SvgImageSource(new Uri(Attachment.Url)) { RasterizePixelWidth = 640 };
                var image = new Image();
                image.Source = source;
                image.Stretch = Stretch.Uniform;
                mainGrid.Child = image;

                detailsGrid.Visibility = Visibility.Collapsed;
            }
            else if (Attachment.Height != 0 && Attachment.Width != 0)
            {
                useNaturalSize = true;

                var imageElement = new ImageElement()
                {
                    ImageWidth = Attachment.Width,
                    ImageHeight = Attachment.Height,
                    ImageUri = new Uri(Attachment.ProxyUrl)
                };

                imageElement.Tapped += Image_Tapped;
                mainGrid.Child = imageElement;

                detailsGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                detailsTransform.Y = 0;
            }

            InvalidateMeasure();
        }

        private void OnMediaOpened(MediaPlayer sender, object args)
        {
            if (naturalSize != null) return;

            var width = sender.PlaybackSession.NaturalVideoWidth;
            var height = sender.PlaybackSession.NaturalVideoHeight;
            if (width != 0 && height != 0)
            {
                this.Dispatcher.AwaitableRunAsync(() =>
                {
                    naturalSize = new Size(width, height);
                    InvalidateMeasure();
                });
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (naturalSize != null)
            {
                double width = naturalSize.Value.Width;
                double height = naturalSize.Value.Height;

                Drawing.ScaleProportions(ref width, ref height, 640, 480);
                Drawing.ScaleProportions(ref width, ref height, Math.Min(constraint.Width, 640), Math.Min(constraint.Height, 480));

                return new Size(width, height);
            }
            else if (useNaturalSize)
            {
                var width = (int)Math.Min(constraint.Width, 480);
                var height = 46;
                return new Size(width, height);
            }

            return base.MeasureOverride(constraint);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            mainGrid.Child = null;
            _timer?.Stop();
        }

        private void _timer_Tick(object sender, object e)
        {
            HideDetails.Begin();
            _timer?.Stop();
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
        }

        private async void OnSaveMenuClick(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            control.IsEnabled = false;

            downloadProgressBar.Visibility = Visibility.Visible;
            downloadProgressBar.IsIndeterminate = true;

            try
            {
                var progress = new Progress<HttpProgress>(p =>
                {
                    if (p.TotalBytesToReceive.HasValue)
                    {
                        downloadProgressBar.Maximum = (double)p.TotalBytesToReceive.Value;
                    }
                    else
                    {
                        downloadProgressBar.IsIndeterminate = true;
                    }

                    downloadProgressBar.Value = p.BytesReceived;
                });

                var extension = Path.GetExtension(Attachment.Url);
                var extensionString = Tools.GetItemTypeFromExtension(extension, _strings.GetString("AttachmentExtensionPlaceholder"));

                var picker = new FileSavePicker()
                {
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    SuggestedFileName = Path.GetFileNameWithoutExtension(Attachment.Url),
                    DefaultFileExtension = extension
                };

                picker.FileTypeChoices.Add($"{extensionString} (*{extension})", new List<string>() { extension });

                var file = await picker.PickSaveFileAsync();
                downloadProgressBar.IsIndeterminate = false;

                if (file != null)
                {
                    await Tools.DownloadToFileAsync(new Uri(Attachment.Url), file, progress);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync(
                    _strings.GetString("AttachmentDownloadFailedTitle"),
                    _strings.GetString("AttachmentDownloadFailedText"));
            }

            control.IsEnabled = true;
            downloadProgressBar.Visibility = Visibility.Collapsed;
        }

        private async void OnShareMenuClick(object sender, RoutedEventArgs e)
        {
            downloadProgressBar.Visibility = Visibility.Visible;
            downloadProgressBar.IsIndeterminate = true;

            try
            {
                var transferManager = DataTransferManager.GetForCurrentView();
                _shareFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Guid.NewGuid()}{Path.GetExtension(Attachment.Url)}");

                var progress = new Progress<HttpProgress>(p =>
                {
                    if (p.TotalBytesToReceive.HasValue)
                    {
                        downloadProgressBar.Maximum = p.TotalBytesToReceive.Value;
                    }
                    else
                    {
                        downloadProgressBar.IsIndeterminate = true;
                    }

                    downloadProgressBar.Value = p.BytesReceived;
                });

                await Tools.DownloadToFileAsync(new Uri(Attachment.Url), _shareFile, progress);
                transferManager.DataRequested += _dataTransferManager_DataRequested;
                DataTransferManager.ShowShareUI();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync(
                    _strings.GetString("AttachmentDownloadFailedTitle"),
                    _strings.GetString("AttachmentDownloadFailedText"));
            }

            downloadProgressBar.Visibility = Visibility.Collapsed;
            downloadProgressBar.IsIndeterminate = false;
        }

        private void _dataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;

            request.Data.Properties.Title = string.Format(_strings.GetString("SharingTitleFormat"), Path.GetFileName(Attachment.Url));
            request.Data.Properties.Description = Path.GetFileName(Attachment.Url);

            request.Data.SetWebLink(new Uri(Attachment.Url));
            request.Data.SetStorageItems(new[] { _shareFile });

            sender.DataRequested -= _dataTransferManager_DataRequested;
        }

        private async void OnOpenMenuClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Attachment.Url));
        }

        private void SpoilerOverlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            spoilerOverlay.Visibility = Visibility.Collapsed;
        }

        private void CopyUrlItem_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(Attachment.Url);
            package.SetWebLink(new Uri(Attachment.Url));
            Clipboard.SetContent(package);
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //ShowDetails.Begin();
            //_timer?.Stop();
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //_timer?.Start();
        }
    }
}
