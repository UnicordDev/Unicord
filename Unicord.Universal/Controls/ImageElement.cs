using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Globalization;
using System.Web;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Controls
{
    public sealed class ImageElement : Control
    {
        public int ImageWidth
        {
            get => (int)GetValue(ImageWidthProperty);
            set => SetValue(ImageWidthProperty, value);
        }

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register("ImageWidth", typeof(int), typeof(ImageElement), new PropertyMetadata(0));

        public int ImageHeight
        {
            get => (int)GetValue(ImageHeightProperty);
            set => SetValue(ImageHeightProperty, value);
        }

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register("ImageHeight", typeof(int), typeof(ImageElement), new PropertyMetadata(0));

        public Uri ImageUri
        {
            get => (Uri)GetValue(ImageUriProperty);
            set => SetValue(ImageUriProperty, value);
        }

        public static readonly DependencyProperty ImageUriProperty =
            DependencyProperty.Register("ImageUri", typeof(Uri), typeof(ImageElement), new PropertyMetadata(null, OnImageChanged));

        public bool IsSpoiler
        {
            get => (bool)GetValue(IsSpoilerProperty);
            set => SetValue(IsSpoilerProperty, value);
        }

        public static readonly DependencyProperty IsSpoilerProperty =
            DependencyProperty.Register("IsSpoiler", typeof(bool), typeof(ImageElement), new PropertyMetadata(false));

        private bool _templated;
        private BitmapImage _img;
        private bool _addedEvent;

        public ImageElement()
        {
            DefaultStyleKey = typeof(ImageElement);
            Unloaded += ImageElement_Unloaded;
        }

        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageElement element)
            {
                if (!element._templated)
                {
                    element._templated = element.ApplyTemplate();
                }

                LoadImage(element);
            }
        }

        private static void LoadImage(ImageElement element)
        {
            var image = element.GetTemplateChild("image") as ImageEx;
            if (image == null || element.ImageUri == null) return;

            double width = element.ImageWidth;
            double height = element.ImageHeight;
            Drawing.ScaleProportions(ref width, ref height, 480, 480);

            var thumbUrl = new UriBuilder(element.ImageUri);
            var query = HttpUtility.ParseQueryString(thumbUrl.Query);
            if (WebPHelpers.ShouldUseWebP)
                query["format"] = "webp";
            query["width"] = ((int)width).ToString(CultureInfo.InvariantCulture);
            query["height"] = ((int)height).ToString(CultureInfo.InvariantCulture);
            thumbUrl.Query = query.ToString();

            element._img = new BitmapImage(thumbUrl.Uri)
            {
                DecodePixelWidth = (int)width,
                DecodePixelHeight = (int)height
            };

            //image.Width = width;
            //image.Height = height;

            if (!App.RoamingSettings.Read(Constants.GIF_AUTOPLAY, true) || NetworkHelper.IsNetworkLimited)
            {
                element._img.AutoPlay = false;
                image.PointerEntered += element.Image_PointerEntered;
                image.PointerExited += element.Image_PointerExited;
            }
            else if (!element._addedEvent)
            {
                element._addedEvent = true;
                Window.Current.VisibilityChanged += element.Current_VisibilityChanged;
            }

            image.Source = element._img;
        }

        protected override void OnApplyTemplate()
        {
            _templated = true;

            if (ImageUri != null)
            {
                LoadImage(this);
            }
        }

        private void Image_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is ImageEx i && i.Source is BitmapImage image)
            {
                image.Stop();
            }
        }

        private void Image_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is ImageEx i && i.Source is BitmapImage image)
            {
                image.Play();
            }
        }

        private void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (_img != null)
            {
                if (e.Visible)
                {
                    _img.Play();
                }
                else
                {
                    _img.Stop();
                }
            }
        }

        private void ImageElement_Unloaded(object sender, RoutedEventArgs e)
        {
            _img = null;
            if (_addedEvent)
            {
                Window.Current.VisibilityChanged -= Current_VisibilityChanged;
            }
        }
    }
}
