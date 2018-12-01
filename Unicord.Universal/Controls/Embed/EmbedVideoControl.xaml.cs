using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls.Embed
{
    public sealed partial class EmbedVideoControl : UserControl
    {
        public EmbedVideoControl()
        {
            this.InitializeComponent();
        }

        public DiscordEmbedVideo Video { get; set; }
        public DiscordEmbedThumbnail Thumbnail { get; set; }

        private void Canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var browser = new WebView(WebViewExecutionMode.SeparateThread)
            {
                Source = Video.Url,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            browser.ContainsFullScreenElementChanged += Browser_ContainsFullScreenElementChanged;
            content.Children.Add(browser);

            posterContainer.Visibility = Visibility.Collapsed;
        }

        private void Browser_ContainsFullScreenElementChanged(WebView sender, object args)
        {
            var page = this.FindParent<MainPage>();
            if (sender.ContainsFullScreenElement)
            {
                if(page != null)
                {
                    page.EnterFullscreen(sender, content);
                }
            }
            else
            {
                page.LeaveFullscreen(sender, content);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var width = Video.Width;
            var height = Video.Height;

            WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, 640, 360);
            WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 360 : (int)constraint.Height);

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

                try { UnloadObject(firstChild); } catch {  }
            }
        }
    }
}
