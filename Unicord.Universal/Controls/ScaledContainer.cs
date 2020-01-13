using System;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls
{
    public sealed class ScaledContentControl : ContentControl
    {
        public int TargetWidth
        {
            get => (int)GetValue(TargetWidthProperty);
            set => SetValue(TargetWidthProperty, value);
        }

        public static readonly DependencyProperty TargetWidthProperty =
            DependencyProperty.Register("TargetWidth", typeof(int), typeof(ScaledContentControl), new PropertyMetadata(0));


        public int TargetHeight
        {
            get => (int)GetValue(TargetHeightProperty);
            set => SetValue(TargetHeightProperty, value);
        }

        public static readonly DependencyProperty TargetHeightProperty =
            DependencyProperty.Register("TargetHeight", typeof(int), typeof(ScaledContentControl), new PropertyMetadata(0));

        private Window _window;

        public ScaledContentControl()
        {
            _window = Window.Current;
            DefaultStyleKey = typeof(ScaledContentControl);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double width = TargetWidth;
            double height = TargetHeight;
            var maxWidth = Math.Min(_window.Bounds.Width, 640);
            var maxHeight = Math.Min(_window.Bounds.Height, 480);

            Drawing.ScaleProportions(ref width, ref height, maxWidth, maxHeight);
            Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? maxWidth : (int)constraint.Width, double.IsInfinity(constraint.Height) ? maxHeight : (int)constraint.Height);

            return new Size(width, height);
        }

    }
}
