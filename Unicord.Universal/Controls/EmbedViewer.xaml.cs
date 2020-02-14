using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Unicord.Universal.Controls.Embeds;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Media.Core;
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
            get { return (DiscordEmbed)GetValue(EmbedProperty); }
            set { SetValue(EmbedProperty, value); }
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
            if (Embed.Type == "image" && Embed.Thumbnail != null)
            {
                var imageElement = new ImageElement() { ImageUri = Embed.Thumbnail.ProxyUrl, ImageWidth = Embed.Thumbnail.Width, ImageHeight = Embed.Thumbnail.Height };
                imageElement.Tapped += ImageElement_Tapped;

                Content = imageElement;
                return;
            }

            if (Embed.Type == "gifv" && Embed.Video != null)
            {
                Logger.Log($"Image: {Embed.Image ?? (object)"null"}");
                var scaleContainer = new ScaledContentControl()
                {
                    TargetWidth = Embed.Video.Width,
                    TargetHeight = Embed.Video.Height,
                };

                _mediaPlayer = new MediaPlayerElement()
                {
                    AreTransportControlsEnabled = false,
                    Source = MediaSource.CreateFromUri(Embed.Video.Url),
                    PosterSource = Embed.Thumbnail != null ? new BitmapImage(Embed.Thumbnail.Url) : null
                };

                _mediaPlayer.Loaded += MediaPlayer_Loaded;
                scaleContainer.Content = _mediaPlayer;

                Content = scaleContainer;

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

                return;
            }

            if (Embed.Color.HasValue)
            {
                var col = Embed.Color.Value;
                Border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));
                Border.BorderThickness = new Thickness(4, 0, 0, 0);
            }

            if (Embed.Image != null)
            {
                var image = new ImageElement() { ImageUri = Embed.Image.ProxyUrl, ImageWidth = Embed.Image.Width, ImageHeight = Embed.Image.Height };
                AddWithRow(image);
            }

            if (Embed.Video != null)
            {
                thumbnail.Visibility = Visibility.Collapsed;
                var video = new EmbedVideoControl() { Embed = Embed, Thumbnail = Embed.Thumbnail, Video = Embed.Video };
                AddWithRow(video);
            }

            if (Embed.Fields?.Any() == true)
            {
                var inline = Embed.Fields.First().Inline;
                var p = inline ? new WrapPanel() : (Panel)new StackPanel();
                foreach (var field in Embed.Fields)
                {
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

                        p = inline ? new WrapPanel() : (Panel)new StackPanel();

                        AddFieldToPanel(p, field);
                    }
                }

                if (p.Children.Count != 0 && !fieldsGrid.Children.Contains(p))
                {
                    AddWithRow(p);
                }
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

        private void ImageElement_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.FindParent<MainPage>()?.ShowAttachmentOverlay(
                Embed.Thumbnail.Url,
                Embed.Thumbnail.Width,
                Embed.Thumbnail.Height,
                open_Click,
                save_Click,
                share_Click);
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
                var extension = Path.GetExtension(Embed.Thumbnail.Url.AbsolutePath);
                var fileName = Path.GetFileNameWithoutExtension(Embed.Thumbnail.Url.AbsolutePath);
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
            catch
            {
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
                var fileName = Path.GetFileName(Embed.Thumbnail.Url.AbsolutePath);
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
            catch
            {
                await UIUtilities.ShowErrorDialogAsync(
                    resources.GetString("AttachmentDownloadFailedTitle"),
                    resources.GetString("AttachmentDownloadFailedText"));
            }

            control.IsEnabled = true;
        }

        private void AddWithRow(FrameworkElement p)
        {
            fieldsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(p, fieldsGrid.RowDefinitions.Count - 1);
            fieldsGrid.Children.Add(p);
        }

        private void AddFieldToPanel(Panel p, DiscordEmbedField field)
        {
            p.Children.Add(new EmbedFieldControl(Embed.Message?.Channel, field));
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
    }
}
