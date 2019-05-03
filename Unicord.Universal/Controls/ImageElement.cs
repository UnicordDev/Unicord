using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

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


        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageElement element)
            {
                if (!element._templated)
                {
                    element._templated = element.ApplyTemplate();
                }
                else
                {
                    LoadImage(element);
                }
            }
        }

        private static void LoadImage(ImageElement element)
        {
            var image = element.GetTemplateChild("image") as Image;
            image.PointerEntered += element.Image_PointerEntered;
            image.PointerExited += element.Image_PointerExited;

            var width = element.ImageWidth;
            var height = element.ImageHeight;
            Drawing.ScaleProportions(ref width, ref height, 640, 480);

            var img = new BitmapImage(new Uri(element.ImageUri.ToString() + $"?width={width}&height={height}"))
            {
                DecodePixelWidth = width,
                DecodePixelHeight = height,
                AutoPlay = false
            };

            image.Source = img;
        }

        private bool _templated;

        public ImageElement()
        {
            DefaultStyleKey = typeof(ImageElement);
        }

        protected override void OnApplyTemplate()
        {
            _templated = true;

            if (ImageUri != null)
            {
                LoadImage(this);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var width = ImageWidth;
            var height = ImageHeight;

            Drawing.ScaleProportions(ref width, ref height, 640, 480);
            Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 480 : (int)constraint.Height);

            var image = GetTemplateChild("image") as Image;

            image.Width = width;
            image.Height = height;

            return new Size(width, height);
        }

        private void Image_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Image i && i.Source is BitmapImage image)
            {
                image.Stop();
            }
        }

        private void Image_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Image i && i.Source is BitmapImage image)
            {
                image.Play();
            }
        }
    }
}
