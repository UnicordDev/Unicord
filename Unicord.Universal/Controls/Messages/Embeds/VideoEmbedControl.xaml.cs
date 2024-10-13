using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web;
using KWebView2;
using Microsoft.UI.Xaml.Controls;
using Unicord.Universal.Models.Messages;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Controls.Messages
{
    public sealed partial class VideoEmbedControl : UserControl
    {
        public EmbedVideoViewModel ViewModel
        {
            get { return (EmbedVideoViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(EmbedVideoViewModel), typeof(VideoEmbedControl), new PropertyMetadata(null));

        public VideoEmbedControl()
        {
            this.InitializeComponent();
            this.Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var mediaPlayerElement = this.FindChild<MediaPlayerElement>();
            if (mediaPlayerElement != null)
            {
                var mediaPlayer = mediaPlayerElement.MediaPlayer;
                var source = mediaPlayerElement.Source;

                if (mediaPlayer.PlaybackSession.CanPause)
                {
                    mediaPlayer.Pause();
                }

                mediaPlayerElement.Source = null;
                mediaPlayerElement.SetMediaPlayer(null);

                if (source is MediaSource mediaSource)
                {
                    mediaSource.Dispose();
                }
                mediaPlayer.Dispose();
            }

            var webView = this.FindChild<WebView2>();
            if (webView != null && webView.CoreWebView2 != null) 
            {
                webView.CoreWebView2.Stop();
                webView.CoreWebView2.IsMuted = true;
                _ = webView.CoreWebView2.TrySuspendAsync();
            }
        }

        // TODO: handle situations where we should be using MediaPlayerElement (including giphy)
        private void Canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var uri = ViewModel.Url;
            var provider = ViewModel.Provider;

            if (ViewModel.Type == "gifv" || ViewModel.Url.Host == "cdn.discordapp.com")
            {
                if (provider == "giphy")
                {
                    uri = new UriBuilder(uri) { Host = "i.giphy.com" }.Uri;
                }

                var mediaPlayerElement = new MediaPlayerElement()
                {
                    TransportControls = new CustomMediaTransportControls(),
                    AreTransportControlsEnabled = ViewModel.Type != "gifv",
                    Source = MediaSource.CreateFromUri(uri),
                    AutoPlay = true,
                };

                mediaPlayerElement.MediaPlayer.IsMuted = true;
                content.Children.Add(mediaPlayerElement);
            }
            else
            {
                if (provider == "youtube")
                {
                    var embedBuilder = new UriBuilder(uri);
                    var query = HttpUtility.ParseQueryString(embedBuilder.Query ?? "");
                    query.Add("autoplay", "1");
                    embedBuilder.Query = query.ToString();

                    uri = embedBuilder.Uri;
                }

                var browser = new UniversalWebView()
                {
                    Source = uri,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                content.Children.Add(browser);
            }

            posterContainer.Opacity = 0;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double width = ViewModel.NaturalWidth;
            double height = ViewModel.NaturalHeight;

            WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, 640, 480);
            WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 480 : (int)constraint.Height);

            return new Size(width, height);
        }
    }
}
