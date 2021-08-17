using System;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls
{
    public sealed class AspectRatioControl : UserControl
    {
        public double TargetAspectRatio
        {
            get => (double)GetValue(TargetAspectRatioProperty);
            set => SetValue(TargetAspectRatioProperty, value);
        }

        public static readonly DependencyProperty TargetAspectRatioProperty =
            DependencyProperty.Register("TargetAspectRatio", typeof(double), typeof(AspectRatioControl), new PropertyMetadata(0));

        public AspectRatioControl()
        {
            DefaultStyleKey = typeof(ScaledContentControl);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            this.Content.Measure(availableSize);

            var desiredSize = this.Content.DesiredSize;
            var size = Math.Max(desiredSize.Width, desiredSize.Height) * TargetAspectRatio;
            return new Size(size, size);
        }

    }
}
