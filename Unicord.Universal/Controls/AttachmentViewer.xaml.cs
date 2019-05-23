using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
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
        DiscordAttachment _attachment;
        private static readonly Lazy<string[]> _mediaExtensions = new Lazy<string[]>(() => new string[] { ".gifv", ".mp4", ".mov", ".webm", ".wmv", ".avi", ".mkv", ".ogv", ".mp3", ".m4a", ".aac", ".wav", ".wma", ".flac", ".ogg", ".oga", ".opus" });

        public AttachmentViewer(DiscordAttachment attachment)
        {
            InitializeComponent();
            _attachment = attachment;

            HorizontalAlignment = HorizontalAlignment.Left;
        }

        private bool _loadDetails = false;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Path.GetFileNameWithoutExtension(_attachment.Url).StartsWith("SPOILER_") && App.RoamingSettings.Read(Constants.ENABLE_SPOILERS, true))
            {
                spoilerOverlay.Visibility = Visibility.Visible;
            }

            var url = _attachment.Url;
            var fileExtension = Path.GetExtension(url);

            if (_mediaExtensions.Value.Contains(fileExtension))
            {
                var mediaPlayer = new MediaPlayerElement()
                {
                    AreTransportControlsEnabled = true,
                    Source = MediaSource.CreateFromUri(new Uri(_attachment.ProxyUrl)),
                    PosterSource = _attachment.Width != 0 ? new BitmapImage(new Uri(_attachment.ProxyUrl + "?format=jpeg")) : null
                };

                mediaPlayer.TransportControls.IsCompact = true;

                if (_attachment.Width == 0)
                {
                    mediaPlayer.VerticalAlignment = VerticalAlignment.Top;
                    mediaPlayer.VerticalContentAlignment = VerticalAlignment.Top;
                    _audioOnly = true;
                }

                mainGrid.Content = mediaPlayer;
            }
            else if (_attachment.Height != 0 && _attachment.Width != 0)
            {
                var imageElement = new ImageElement()
                {
                    ImageWidth = _attachment.Width,
                    ImageHeight = _attachment.Height,
                    ImageUri = new Uri(_attachment.ProxyUrl)
                };

                imageElement.Tapped += Image_Tapped;
                mainGrid.Content = imageElement;
            }
            else
            {
                _loadDetails = true;
                Bindings.Update();
            }

            if (!_loadDetails)
                Background = null;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (_attachment.Width != 0)
            {
                var width = _attachment.Width;
                var height = _attachment.Height;

                Drawing.ScaleProportions(ref width, ref height, 640, 480);
                Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 480 : (int)constraint.Height);

                mainGrid.Width = width;
                mainGrid.Height = height;

                return new Size(width, height);
            }
            else if (_audioOnly)
            {
                var width = (int)Math.Min(constraint.Width, 480);
                var height = 42;

                mainGrid.Width = width;
                mainGrid.Height = height;

                return new Size(width, height);
            }

            return base.MeasureOverride(constraint);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            //var mediaPlayer = mainGrid.FindChild<MediaPlayerControl>();
            //mediaPlayer?.meda.Pause();

            mainGrid.Content = null;
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.FindParent<MainPage>()?.ShowAttachmentOverlay(
                new Uri(_attachment.ProxyUrl),
                _attachment.Width,
                _attachment.Height,
                openMenuItem_Click,
                saveMenuItem_Click,
                shareMenuItem_Click);
        }

        private async void saveMenuItem_Click(object sender, RoutedEventArgs e)
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

                var picker = new FileSavePicker()
                {
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    SuggestedFileName = Path.GetFileNameWithoutExtension(_attachment.Url),
                    DefaultFileExtension = Path.GetExtension(_attachment.Url)
                };

                picker.FileTypeChoices.Add($"Attachment Extension (*{Path.GetExtension(_attachment.Url)})", new List<string>() { Path.GetExtension(_attachment.Url) });

                var file = await picker.PickSaveFileAsync();
                downloadProgressBar.IsIndeterminate = false;

                if (file != null)
                {
                    await Tools.DownloadToFileWithProgressAsync(new Uri(_attachment.Url), file, progress);
                }
            }
            catch (Exception)
            {
                await UIUtilities.ShowErrorDialogAsync(
                    "Failed to download attachment",
                    "Something went wrong downloading that attachment, maybe try again later?");
            }

            control.IsEnabled = true;
            downloadProgressBar.Visibility = Visibility.Collapsed;
        }

        StorageFile _shareFile;
        private bool _audioOnly;

        private async void shareMenuItem_Click(object sender, RoutedEventArgs e)
        {
            downloadProgressBar.Visibility = Visibility.Visible;
            downloadProgressBar.IsIndeterminate = true;

            try
            {

                var transferManager = DataTransferManager.GetForCurrentView();
                _shareFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Guid.NewGuid()}{Path.GetExtension(_attachment.Url)}");

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

                await Tools.DownloadToFileWithProgressAsync(new Uri(_attachment.Url), _shareFile, progress);
                transferManager.DataRequested += _dataTransferManager_DataRequested;
                DataTransferManager.ShowShareUI();
            }
            catch (Exception)
            {
                await UIUtilities.ShowErrorDialogAsync(
                    "Failed to download attachment",
                    "Something went wrong downloading that attachment, maybe try again later?");
            }

            downloadProgressBar.Visibility = Visibility.Collapsed;
            downloadProgressBar.IsIndeterminate = false;
        }

        private void _dataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;

            request.Data.Properties.Title = $"Sharing {Path.GetFileName(_attachment.Url)}";
            request.Data.Properties.Description = Path.GetFileName(_attachment.Url);

            request.Data.SetWebLink(new Uri(_attachment.Url));
            request.Data.SetStorageItems(new[] { _shareFile });

            sender.DataRequested -= _dataTransferManager_DataRequested;
        }

        private async void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(_attachment.Url));
        }

        private void SpoilerOverlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            spoilerOverlay.Visibility = Visibility.Collapsed;
        }

        private void CopyUrlItem_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(_attachment.Url);
            package.SetWebLink(new Uri(_attachment.Url));
            Clipboard.SetContent(package);
        }
    }
}
