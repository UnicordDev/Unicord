using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Unicord.Universal.Controls.Embeds;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Controls
{
    public sealed partial class EmbedViewer : UserControl
    {
        private bool _mediaPlayerPlaying = false;
        private MediaPlayerElement _mediaPlayer;

        public DiscordEmbed Embed
        {
            get => (DiscordEmbed)GetValue(EmbedProperty);
            set => SetValue(EmbedProperty, value);
        }

        public static readonly DependencyProperty EmbedProperty =
            DependencyProperty.Register("Embed", typeof(DiscordEmbed), typeof(EmbedViewer), new PropertyMetadata(null));

        public EmbedViewer()
        {
            InitializeComponent();
        }

        public EmbedViewer(DiscordMessage m, DiscordEmbed embed)
        {
            Embed = embed;
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Embed.Type == "video")
                Logger.Log(Embed.Video.Url);

            if (Embed.Type == "image" && Embed.Thumbnail != null)
            {
                var imageElement = new ImageElement() { ImageUri = Embed.Thumbnail.ProxyUrl, ImageWidth = Embed.Thumbnail.Width, ImageHeight = Embed.Thumbnail.Height };
                imageElement.Tapped += OnImageTapped;

                Content = imageElement;
                return;
            }

            if (Embed.Video != null && (Embed.Type == "gifv" || (Embed.Type == "video" && Embed.Video.Url.Host == "cdn.discordapp.com")))
            {
                Logger.Log($"Image: {Embed.Image ?? (object)"null"}");
                var scaleContainer = new ScaledContentControl()
                {
                    TargetWidth = Embed.Video.Width,
                    TargetHeight = Embed.Video.Height
                };

                var mediaBorder = new Border();
                var controls = new CustomMediaTransportControls();
                _mediaPlayer = new MediaPlayerElement()
                {
                    AreTransportControlsEnabled = Embed.Type != "gifv",
                    Source = MediaSource.CreateFromUri(Embed.Video.Url),
                    PosterSource = Embed.Thumbnail != null ? new BitmapImage(Embed.Thumbnail.Url) : null,
                    TransportControls = controls
                };

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {
                    _mediaPlayer.TransportControls.ShowAndHideAutomatically = true;
                }

                if (Embed.Type == "gifv")
                {
                    _mediaPlayer.Loaded += MediaPlayer_Loaded;

                    if (App.RoamingSettings.Read(Constants.GIF_AUTOPLAY, true) && !NetworkHelper.IsNetworkLimited)
                    {
                        Window.Current.VisibilityChanged += OnWindowVisibilityChanged;
                        _mediaPlayer.AutoPlay = true;
                    }
                    else
                    {
                        _mediaPlayer.Tapped += OnMediaPlayerTapped;
                        _mediaPlayer.AutoPlay = false;
                    }
                }
                else
                {
                    controls.FullWindowRequested += async (o, ev) =>
                    {
                        if (_mediaPlayer.IsFullWindow)
                        {
                            _mediaPlayer.IsFullWindow = !_mediaPlayer.IsFullWindow;
                            await FullscreenService.GetForCurrentView()
                                                   .LeaveFullscreenAsync(_mediaPlayer, mediaBorder);
                        }
                        else
                        {
                            await FullscreenService.GetForCurrentView()
                                                   .EnterFullscreenAsync(_mediaPlayer, mediaBorder);
                            _mediaPlayer.IsFullWindow = !_mediaPlayer.IsFullWindow;
                        }
                        

                    };

                    mediaBorder.Child = _mediaPlayer;
                    scaleContainer.Content = mediaBorder;
                    Content = scaleContainer;

                    return;
                }
            }

            bool inline = false;
            Panel p = null;
            for (var i = 0; i < Embed.Fields.Count; i++)
            {
                var field = Embed.Fields[i];
                if (i == 0)
                {
                    inline = field.Inline;
                    p = inline ? new WrapPanel() { StretchChild = StretchChild.Last } : (Panel)new StackPanel();
                }

                if (field.Inline == inline)
                {
                    AddFieldToPanel(p, field);
                }
                else
                {
                    if (p.Children.Count != 0)
                    {
                        AddWithRow(p);
                    }

                    inline = field.Inline;
                    p = inline ? new WrapPanel() { StretchChild = StretchChild.Last } : (Panel)new StackPanel();

                    AddFieldToPanel(p, field);
                }
            }

            if (p != null && p.Children.Count != 0 && !FieldsGrid.Children.Contains(p))
            {
                AddWithRow(p);
            }

            if (Embed.Image != null)
            {
                var image = new ImageElement() { ImageUri = Embed.Image.ProxyUrl, ImageWidth = Embed.Image.Width, ImageHeight = Embed.Image.Height };
                image.Tapped += OnImageTapped;
                AddWithRow(image);
            }

            if (Embed.Video != null)
            {
                if (DescriptionText != null && Embed.Type != "rich")
                    DescriptionText.Visibility = Visibility.Collapsed;
                ThumbnailImage.Visibility = Visibility.Collapsed;
                var video = new EmbedVideoControl() { Embed = Embed, Thumbnail = Embed.Thumbnail, Video = Embed.Video };
                AddWithRow(video);
            }



            Bindings.Update();
        }

        private void OnMediaPlayerTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_mediaPlayerPlaying)
            {
                _mediaPlayer.MediaPlayer.Pause();
            }
            else
            {
                _mediaPlayer.MediaPlayer.Play();
            }

            _mediaPlayerPlaying = !_mediaPlayerPlaying;
        }

        private void OnWindowVisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                if (e.Visible)
                {
                    _mediaPlayer.MediaPlayer.Play();
                }
                else
                {
                    _mediaPlayer.MediaPlayer.Pause();
                }
            }
        }

        private void MediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is MediaPlayerElement element)
            {
                element.MediaPlayer.IsMuted = true;
                element.MediaPlayer.IsLoopingEnabled = true;
            }
        }

        private async void open_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(Embed.Thumbnail.Url);
        }

        private async void save_Click(object sender, RoutedEventArgs e)
        {
            var resources = ResourceLoader.GetForCurrentView("Controls");
            var control = sender as Control;
            control.IsEnabled = false;

            try
            {
                var extension = Path.GetExtension(Embed.Thumbnail.Url.ToString());
                var fileName = Path.GetFileNameWithoutExtension(Embed.Thumbnail.Url.ToString());
                var picker = new FileSavePicker()
                {
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    SuggestedFileName = fileName,
                    DefaultFileExtension = extension
                };

                picker.FileTypeChoices.Add(string.Format(resources.GetString("AttachmentExtensionFormat"), extension), new List<string>() { extension });

                var file = await picker.PickSaveFileAsync();

                if (file != null)
                {
                    await Tools.DownloadToFileAsync(Embed.Thumbnail.Url, file);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync(
                    resources.GetString("AttachmentDownloadFailedTitle"),
                    resources.GetString("AttachmentDownloadFailedText"));
            }

            control.IsEnabled = true;
        }

        private async void share_Click(object sender, RoutedEventArgs e)
        {
            var resources = ResourceLoader.GetForCurrentView("Controls");
            var control = sender as Control;
            control.IsEnabled = false;

            try
            {
                var fileName = Path.GetFileName(Embed.Thumbnail.Url.ToString());
                var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

                var dataTransferManager = DataTransferManager.GetForCurrentView();
                void DataRequested(DataTransferManager manager, DataRequestedEventArgs args)
                {
                    var data = args.Request.Data;
                    data.Properties.Title = string.Format(resources.GetString("SharingTitleFormat"), fileName);
                    data.Properties.Description = fileName;

                    data.SetWebLink(Embed.Thumbnail.Url);
                    data.SetStorageItems(new[] { file });
                    dataTransferManager.DataRequested -= DataRequested;
                }

                await Tools.DownloadToFileAsync(Embed.Thumbnail.Url, file);

                dataTransferManager.DataRequested += DataRequested;
                DataTransferManager.ShowShareUI();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync(
                    resources.GetString("AttachmentDownloadFailedTitle"),
                    resources.GetString("AttachmentDownloadFailedText"));
            }

            control.IsEnabled = true;
        }

        private void AddWithRow(FrameworkElement p)
        {
            FieldsGrid.Visibility = Visibility.Visible;
            FieldsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(p, FieldsGrid.RowDefinitions.Count - 1);
            FieldsGrid.Children.Add(p);
        }

        private void AddFieldToPanel(Panel p, DiscordEmbedField field)
        {
            p.Children.Add(new EmbedFieldControl(Embed.Owner?.Channel, field));
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Content is ScaledContentControl scc && scc.Content is MediaPlayerElement element)
            {
                element.Source = null;
                Window.Current.VisibilityChanged -= OnWindowVisibilityChanged;
            }

            _mediaPlayer = null;
        }

        private void Thumbnail_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            (sender as Image).Visibility = Visibility.Collapsed;
        }

        private async void ThumbnailImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Embed.Thumbnail != null)
                await OverlayService.GetForCurrentView()
                                    .ShowOverlayAsync<AttachmentOverlayPage>(Embed.Thumbnail);
        }

        private async void OnImageTapped(object sender, TappedRoutedEventArgs e)
        {
            if (Embed.Image != null)
                await OverlayService.GetForCurrentView()
                                    .ShowOverlayAsync<AttachmentOverlayPage>(Embed.Image);
        }
    }
}
