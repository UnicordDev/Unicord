using System;
using System.Collections.Generic;
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
            get { return (int)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register("ImageWidth", typeof(int), typeof(ImageElement), new PropertyMetadata(0));


        public int ImageHeight
        {
            get { return (int)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register("ImageHeight", typeof(int), typeof(ImageElement), new PropertyMetadata(0));


        public Uri ImageUri
        {
            get { return (Uri)GetValue(ImageUriProperty); }
            set { SetValue(ImageUriProperty, value); }
        }

        public static readonly DependencyProperty ImageUriProperty =
            DependencyProperty.Register("ImageUri", typeof(Uri), typeof(ImageElement), new PropertyMetadata(null));

        public ImageElement()
        {
            this.DefaultStyleKey = typeof(ImageElement);
        }

        protected override void OnApplyTemplate()
        {
            var image = GetTemplateChild("image") as Image;
            var width = ImageWidth;
            var height = ImageHeight;
            Drawing.ScaleProportions(ref width, ref height, 640, 360);

            var img = new BitmapImage(ImageUri)
            {
                DecodePixelWidth = width,
                DecodePixelHeight = height
            };

            image.Source = img;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var width = ImageWidth;
            var height = ImageHeight;

            Drawing.ScaleProportions(ref width, ref height, 640, 360);
            Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 360 : (int)constraint.Height);

            var image = GetTemplateChild("image") as Image;

            image.Width = width;
            image.Height = height;

            return new Size(width, height);
        }
    }
}
