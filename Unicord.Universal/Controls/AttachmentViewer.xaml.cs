using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus.Entities;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Media.Core;
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

        private bool _audioOnly;

        private DispatcherTimer _timer;
        private StorageFile _shareFile;

        public DiscordAttachment Attachment
        {
            get { return (DiscordAttachment)GetValue(AttachmentProperty); }
            set { SetValue(AttachmentProperty, value); }
        }
        
        public static readonly DependencyProperty AttachmentProperty =
            DependencyProperty.Register("Attachment", typeof(DiscordAttachment), typeof(AttachmentViewer), new PropertyMetadata(null));

        public AttachmentViewer()
        {
            InitializeComponent();
        }

        public AttachmentViewer(DiscordAttachment attachment)
        {
            InitializeComponent();
            Attachment = attachment;
            DataContext = attachment;
            HorizontalAlignment = HorizontalAlignment.Left;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Path.GetFileNameWithoutExtension(Attachment.Url).StartsWith("SPOILER_") && App.RoamingSettings.Read(Constants.ENABLE_SPOILERS, true))
            {
                spoilerOverlay.Visibility = Visibility.Visible;
            }

            var url = Attachment.Url;
            var fileExtension = Path.GetExtension(url);

            if (_mediaExtensions.Contains(fileExtension))
            {
                var mediaPlayer = new MediaPlayerElement()
                {
                    AreTransportControlsEnabled = true,
                    Source = MediaSource.CreateFromUri(new Uri(Attachment.ProxyUrl)),
                    PosterSource = Attachment.Width != 0 ? new BitmapImage(new Uri(Attachment.ProxyUrl + "?format=jpeg")) : null
                };

                mediaPlayer.TransportControls.Style = (Style)App.Current.Resources["MediaTransportControlsStyle"];
                mediaPlayer.TransportControls.IsCompact = true;

                if (Attachment.Width == 0)
                {
                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                    {
                        mediaPlayer.TransportControls.ShowAndHideAutomatically = false;
                    }

                    mediaPlayer.VerticalAlignment = VerticalAlignment.Top;
                    mediaPlayer.VerticalContentAlignment = VerticalAlignment.Top;
                    detailsGrid.Visibility = Visibility.Collapsed;
                    _audioOnly = true;
                }

                mainGrid.Content = mediaPlayer;

                _timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(3) };
                _timer.Tick += _timer_Tick;
            }
            else if (Attachment.Height != 0 && Attachment.Width != 0)
            {
                var imageElement = new ImageElement()
                {
                    ImageWidth = Attachment.Width,
                    ImageHeight = Attachment.Height,
                    ImageUri = new Uri(Attachment.ProxyUrl)
                };

                imageElement.Tapped += Image_Tapped;
                mainGrid.Content = imageElement;

                _timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(3) };
                _timer.Tick += _timer_Tick;
            }
            else
            {
                detailsTransform.Y = 0;
                grid.PointerEntered -= Grid_PointerEntered;
                grid.PointerExited -= Grid_PointerExited;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Attachment.Width != 0)
            {
                double width = Attachment.Width;
                double height = Attachment.Height;

                Drawing.ScaleProportions(ref width, ref height, 640, 480);
                Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 480 : (int)constraint.Height);

                mainGrid.Width = width;
                mainGrid.Height = height;

                Clip = new RectangleGeometry() { Rect = new Rect(0, 0, width, height) };

                return new Size(width, height);
            }
            else if (_audioOnly)
            {
                var width = (int)Math.Min(constraint.Width, 480);
                var height = 42;

                mainGrid.Width = width;
                mainGrid.Height = height;

                Clip = new RectangleGeometry() { Rect = new Rect(0, 0, width, height) };

                return new Size(width, height);
            }

            return base.MeasureOverride(constraint);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            //var mediaPlayer = mainGrid.FindChild<MediaPlayerControl>();
            //mediaPlayer?.meda.Pause();

            mainGrid.Content = null;
            _timer?.Stop();
        }

        private void _timer_Tick(object sender, object e)
        {
            HideDetails.Begin();
            _timer?.Stop();
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.FindParent<MainPage>()?.ShowAttachmentOverlay(
                new Uri(Attachment.ProxyUrl),
                Attachment.Width,
                Attachment.Height,
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
                    SuggestedFileName = Path.GetFileNameWithoutExtension(Attachment.Url),
                    DefaultFileExtension = Path.GetExtension(Attachment.Url)
                };

                picker.FileTypeChoices.Add($"Attachment Extension (*{Path.GetExtension(Attachment.Url)})", new List<string>() { Path.GetExtension(Attachment.Url) });

                var file = await picker.PickSaveFileAsync();
                downloadProgressBar.IsIndeterminate = false;

                if (file != null)
                {
                    await Tools.DownloadToFileWithProgressAsync(new Uri(Attachment.Url), file, progress);
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

        private async void shareMenuItem_Click(object sender, RoutedEventArgs e)
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

                await Tools.DownloadToFileWithProgressAsync(new Uri(Attachment.Url), _shareFile, progress);
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

            request.Data.Properties.Title = $"Sharing {Path.GetFileName(Attachment.Url)}";
            request.Data.Properties.Description = Path.GetFileName(Attachment.Url);

            request.Data.SetWebLink(new Uri(Attachment.Url));
            request.Data.SetStorageItems(new[] { _shareFile });

            sender.DataRequested -= _dataTransferManager_DataRequested;
        }

        private async void openMenuItem_Click(object sender, RoutedEventArgs e)
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
