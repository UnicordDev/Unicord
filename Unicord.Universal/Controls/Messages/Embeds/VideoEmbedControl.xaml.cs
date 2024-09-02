using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.QueryStringDotNET;
using Unicord.Universal.Models.Messages;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
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
                    AreTransportControlsEnabled = false,
                    Source = MediaSource.CreateFromUri(uri),
                    AutoPlay = true,
                    CornerRadius = new CornerRadius(4)
                };

                mediaPlayerElement.MediaPlayer.IsMuted = true;
                content.Children.Insert(0, mediaPlayerElement);
            }
            else
            {
                if (provider == "youtube")
                {
                    var embedBuilder = new UriBuilder(uri);
                    var query = QueryString.Parse(System.Net.WebUtility.UrlDecode(embedBuilder.Query ?? "").Trim('?'));
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
