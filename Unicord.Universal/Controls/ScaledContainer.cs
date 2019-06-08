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


        public ScaledContentControl()
        {
            DefaultStyleKey = typeof(ScaledContentControl);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var width = TargetWidth;
            var height = TargetHeight;

            Drawing.ScaleProportions(ref width, ref height, 640, 480);
            Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 480 : (int)constraint.Height);

            return new Size(width, height);
        }

    }
}
