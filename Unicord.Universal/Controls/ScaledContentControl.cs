using System;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls
{
    public sealed class ScaledContentControl : ContentControl
    {
        public double TargetWidth
        {
            get => (double)GetValue(TargetWidthProperty);
            set => SetValue(TargetWidthProperty, value);
        }

        public static readonly DependencyProperty TargetWidthProperty =
            DependencyProperty.Register("TargetWidth", typeof(double), typeof(ScaledContentControl), new PropertyMetadata(double.NaN, OnWidthHeightPropertyChanged));

        public double TargetHeight
        {
            get => (double)GetValue(TargetHeightProperty);
            set => SetValue(TargetHeightProperty, value);
        }

        public static readonly DependencyProperty TargetHeightProperty =
            DependencyProperty.Register("TargetHeight", typeof(double), typeof(ScaledContentControl), new PropertyMetadata(double.NaN, OnWidthHeightPropertyChanged));

        public bool ForceSize
        {
            get { return (bool)GetValue(ForceSizeProperty); }
            set { SetValue(ForceSizeProperty, value); }
        }

        public static readonly DependencyProperty ForceSizeProperty =
            DependencyProperty.Register("ForceSize", typeof(bool), typeof(ScaledContentControl), new PropertyMetadata(false, OnWidthHeightPropertyChanged));

        public bool UseFullscreen
        {
            get { return (bool)GetValue(UseFullscreenProperty); }
            set { SetValue(UseFullscreenProperty, value); }
        }

        public static readonly DependencyProperty UseFullscreenProperty =
            DependencyProperty.Register("UseFullscreen", typeof(bool), typeof(ScaledContentControl), new PropertyMetadata(false, OnWidthHeightPropertyChanged));

        private static void OnWidthHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ScaledContentControl)d;
            control.InvalidateMeasure();
            control.InvalidateArrange();
            control.UpdateLayout();
        }

        private Window root;

        public ScaledContentControl()
        {
            DefaultStyleKey = typeof(ScaledContentControl);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (root == null)
                root = Window.Current;

            double width = TargetWidth;
            double height = TargetHeight;

            if (double.IsNaN(width) || double.IsNaN(height) || width <= 0 || height <= 0)
                return base.MeasureOverride(constraint);

            var horizontalMargin = UseFullscreen ? 0 : 80;
            var verticalMargin = UseFullscreen ? 0 : 160;

            var maxWidth = Math.Min(root.Bounds.Width - horizontalMargin, Math.Min(MaxWidth, constraint.Width));
            var maxHeight = Math.Min(root.Bounds.Height - verticalMargin, Math.Min(MaxHeight, constraint.Height));

            if (UseFullscreen)
            {
                // Scale to cover the available area (fill both width and height) so panning can traverse full image
                var scaleX = maxWidth / width;
                var scaleY = maxHeight / height;
                var scale = Math.Max(scaleX, scaleY);

                // Apply scale
                width = width * scale;
                height = height * scale;
            }
            else
            {
                // Normal mode: Fit within maxWidth/maxHeight (contain)
                // only scale down if the image is larger than the available space
                if (width > maxWidth || height > maxHeight)
                {
                    var scaleX = maxWidth / width;
                    var scaleY = maxHeight / height;
                    var scale = Math.Min(scaleX, scaleY);
                    width *= scale;
                    height *= scale;
                }
            }

            if (ForceSize && Content is FrameworkElement element)
            {
                element.Width = width;
                element.Height = height;
            }

            if (Content is UIElement child)
            {
                child.Measure(new Size(width, height));
            }

            return new Size(width, height);
        }

    }
}
