using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Controls.Embeds
{
    public sealed partial class EmbedVideoControl : UserControl
    {
        public EmbedVideoControl()
        {
            InitializeComponent();
        }

        public DiscordEmbed Embed { get; set; }
        public DiscordEmbedVideo Video { get; set; }
        public DiscordEmbedThumbnail Thumbnail { get; set; }

        private void Canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var uri = Video.Url;
            var provider = Embed?.Provider?.Name.ToLowerInvariant();

            if (provider == "giphy")
            {
                var builder = new UriBuilder(uri) { Host = "i.giphy.com" };
                uri = builder.Uri;
            }

            if (provider == "youtube")
            {
                var embedBuilder = new UriBuilder(uri);
                var query = QueryString.Parse(System.Net.WebUtility.UrlDecode(embedBuilder.Query ?? "").Trim('?'));
                query.Add("autoplay", "1");
                embedBuilder.Query = query.ToString();

                uri = embedBuilder.Uri;
            }

            var executionMode = WebViewExecutionMode.SameThread;
            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.WebViewExecutionMode", "SeparateProcess"))
            {
                executionMode = WebViewExecutionMode.SeparateProcess;
            }

            var browser = new WebView(executionMode)
            {
                Source = uri,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            browser.ContainsFullScreenElementChanged += Browser_ContainsFullScreenElementChanged;

            content.Children.Add(browser);

            posterContainer.Visibility = Visibility.Collapsed;
        }

        private void Browser_ContainsFullScreenElementChanged(WebView sender, object args)
        {
            var service = FullscreenService.GetForCurrentView();
            if (sender.ContainsFullScreenElement)
            {
                service.EnterFullscreen(sender, content);
            }
            else
            {
                service.LeaveFullscreen(sender, content);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var width = Video.Width;
            var height = Video.Height;

            WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, 640, 480);
            WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 480 : (int)constraint.Height);

            return new Size(width, height);
        }

        private void UserControl_FocusDisengaged(Control sender, FocusDisengagedEventArgs args)
        {
            CleanupWebView();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupWebView();
        }

        private void CleanupWebView()
        {
            var firstChild = content.Children.OfType<WebView>().FirstOrDefault();
            if (firstChild is WebView)
            {
                content.Children.Remove(firstChild);
                posterContainer.Visibility = Visibility.Visible;

                firstChild.Navigate(new Uri("about:blank"));

                try { UnloadObject(firstChild); } catch { }
            }
        }
    }
}
