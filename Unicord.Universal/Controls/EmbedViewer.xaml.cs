using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Unicord.Universal.Controls.Embeds;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


namespace Unicord.Universal.Controls
{
    public sealed partial class EmbedViewer : UserControl
    {
        private DiscordEmbed _embed;
        private DiscordChannel _channel;
        private bool _showThumbnail;
        private bool _mediaPlayerPlaying = false;
        private MediaPlayerElement _mediaPlayer;        

        public EmbedViewer()
        {
            InitializeComponent();
        }

        public EmbedViewer(DiscordMessage m, DiscordEmbed embed)
        {
            _embed = embed;
            _channel = m.Channel;
            InitializeComponent();
            thumbnail.Visibility = Visibility.Visible;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_embed.Type == "image" && _embed.Thumbnail != null)
            {
                var imageElement = new ImageElement() { ImageUri = _embed.Thumbnail.ProxyUrl, ImageWidth = _embed.Thumbnail.Width, ImageHeight = _embed.Thumbnail.Height };
                imageElement.Tapped += ImageElement_Tapped;

                Content = imageElement;
                return;
            }

            if (_embed.Type == "gifv" && _embed.Video != null)
            {
                Logger.Log($"Image: {_embed.Image ?? (object)"null"}");
                var scaleContainer = new ScaledContentControl()
                {
                    TargetWidth = _embed.Video.Width,
                    TargetHeight = _embed.Video.Height,
                };

                _mediaPlayer = new MediaPlayerElement()
                {
                    AreTransportControlsEnabled = false,
                    Source = MediaSource.CreateFromUri(_embed.Video.Url),
                    PosterSource = _embed.Thumbnail != null ? new BitmapImage(_embed.Thumbnail.Url) : null
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

            if (_embed.Thumbnail != null)
            {
                thumbnail.Visibility = Visibility.Visible;
            }

            if (_embed.Color.HasValue)
            {
                var col = _embed.Color.Value;
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));
            }

            if (_embed.Image != null)
            {
                var image = new ImageElement() { ImageUri = _embed.Image.ProxyUrl, ImageWidth = _embed.Image.Width, ImageHeight = _embed.Image.Height };
                AddWithRow(image);
            }

            if (_embed.Video != null)
            {
                thumbnail.Visibility = Visibility.Collapsed;
                var video = new EmbedVideoControl() { Embed = _embed, Thumbnail = _embed.Thumbnail, Video = _embed.Video };
                AddWithRow(video);
            }

            if (_embed.Fields?.Any() == true)
            {
                var inline = _embed.Fields.First().Inline;
                var p = inline ? new WrapPanel() : (Panel)new StackPanel();
                foreach (var field in _embed.Fields)
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
                _embed.Thumbnail.ProxyUrl,
                _embed.Thumbnail.Width,
                _embed.Thumbnail.Height,
                open_Click,
                save_Click,
                share_Click);
        }

        private async void open_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(_embed.Thumbnail.Url);
        }

        private async void save_Click(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            control.IsEnabled = false;

            try
            {
                var extension = Path.GetExtension(_embed.Thumbnail.Url.AbsolutePath);
                var fileName = Path.GetFileNameWithoutExtension(_embed.Thumbnail.Url.AbsolutePath);
                var picker = new FileSavePicker()
                {
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    SuggestedFileName = fileName,
                    DefaultFileExtension = extension
                };

                picker.FileTypeChoices.Add($"Attachment Extension (*{extension})", new List<string>() { extension });

                var file = await picker.PickSaveFileAsync();

                if (file != null)
                {
                    await Tools.DownloadToFileAsync(_embed.Thumbnail.Url, file);
                }
            }
            catch
            {
                await UIUtilities.ShowErrorDialogAsync(
                    "Failed to download image",
                    "Something went wrong downloading that image, maybe try again later?");
            }

            control.IsEnabled = true;
        }

        private async void share_Click(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            control.IsEnabled = false;

            try
            {
                var fileName = Path.GetFileName(_embed.Thumbnail.Url.AbsolutePath);
                var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

                var dataTransferManager = DataTransferManager.GetForCurrentView();
                void DataRequested(DataTransferManager manager, DataRequestedEventArgs args)
                {
                    var data = args.Request.Data;
                    data.Properties.Title = $"Sharing {fileName}";
                    data.Properties.Description = fileName;

                    data.SetWebLink(_embed.Thumbnail.Url);
                    data.SetStorageItems(new[] { file });
                    dataTransferManager.DataRequested -= DataRequested;
                }

                await Tools.DownloadToFileAsync(_embed.Thumbnail.Url, file);

                dataTransferManager.DataRequested += DataRequested;
                DataTransferManager.ShowShareUI();
            }
            catch
            {
                await UIUtilities.ShowErrorDialogAsync(
                    "Failed to download image",
                    "Something went wrong downloading that image, maybe try again later?");
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
            p.Children.Add(new EmbedFieldControl(_channel, field));
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Content is ScaledContentControl scc && scc.Content is MediaPlayerElement element)
            {
                element.Source = null;
                try { UnloadObject(element); } catch { }
                Window.Current.VisibilityChanged -= OnWindowVisibilityChanged;
            }

            _mediaPlayer = null;
        }
    }
}
