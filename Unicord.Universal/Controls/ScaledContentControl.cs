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

            if (double.IsNaN(width) || double.IsNaN(height))
                return base.MeasureOverride(constraint);

            var maxWidth = Math.Min(root.Bounds.Width, MaxWidth);
            var maxHeight = Math.Min(root.Bounds.Height - 32, MaxHeight);

            Drawing.ScaleProportions(ref width, ref height, maxWidth, maxHeight);
            Drawing.ScaleProportions(ref width, ref height, Math.Min(constraint.Width, maxWidth), Math.Min(constraint.Height, maxHeight));

            if (ForceSize && Content is FrameworkElement element)
            {
                element.Width = width;
                element.Height = height;
            }

            return new Size(width, height);
        }

    }
}
