using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using static Unicord.Constants;
using ColorHelper = Microsoft.Toolkit.Uwp.Helpers.ColorHelper;

namespace Unicord.Universal.Converters
{
    public class ColourBrushConverter : DependencyObject, IValueConverter
    {
        private Dictionary<long, SolidColorBrush> _brushCache
         = new Dictionary<long, SolidColorBrush>();

        public Color DefaultBackgroundColour
        {
            get { return (Color)GetValue(DefaultBackgroundColourProperty); }
            set { SetValue(DefaultBackgroundColourProperty, value); }
        }

        public static readonly DependencyProperty DefaultBackgroundColourProperty =
            DependencyProperty.Register("DefaultBackgroundColour", typeof(Color), typeof(ColourBrushConverter), new PropertyMetadata(Colors.Black));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Brush b)
                return b;

            var background = parameter is Color ? (Color)parameter : DefaultBackgroundColour;
            var col = new DiscordColor();
            if (value is DiscordColor)
                col = (DiscordColor)value;

            if (value is Optional<DiscordColor> opt && opt.HasValue)
                col = opt.Value;

            if (col.Value == 0)
                return null;

            var winCol = Color.FromArgb(255, col.R, col.G, col.B);

            if (!App.RoamingSettings.Read(ADJUST_ROLE_COLOURS, true))
                return GetOrCreateBrush(background, winCol);

            var hslCol = ColorHelper.ToHsl(winCol);
            var contrast = App.RoamingSettings.Read(MINIMUM_CONTRAST, MINIMUM_CONTRAST_DEFAULT);
            var backgroundCol = ColorHelper.ToHsl(background);

            var targetCol = winCol;

            var dark = backgroundCol.L < 0.5;
            var luma = hslCol.L;

            var targetLuma = 0.0;

            if (dark)
                targetLuma = (contrast * (backgroundCol.L + 0.05)) - 0.05;
            else
                targetLuma = ((backgroundCol.L + 0.05) / contrast) - 0.05;

            if (dark ? luma < targetLuma : luma > targetLuma)
                targetCol = TargetLuma(hslCol, targetLuma);


            return GetOrCreateBrush(background, winCol, targetCol);
        }

        private Color TargetLuma(HslColor col, double target)
        {
            var s = col.S;
            var min = 0.0;
            var max = 1.0;

            s *= Math.Pow(col.L > 0.5 ? -col.L : col.L - 1, 7) + 1;

            var d = (max - min) / 2;
            var mid = min + d;

            for (; d > 1.0 / 65536.0; d /= 2, mid = min + d)
            {
                if (mid > target)
                    max = mid;
                else
                    min = mid;
            }

            return ColorHelper.FromHsl(col.H, s, mid, col.A);
        }

        private SolidColorBrush GetOrCreateBrush(Color background, Color colourKey, Color? value = null)
        {
            var bg = ColorHelper.ToInt(background);
            var fg = ColorHelper.ToInt(colourKey);
            var argb = (((long)bg) << 32) | (long)fg;
            if (_brushCache.TryGetValue(argb, out var brush))
                return brush;

            return _brushCache[argb] = new SolidColorBrush(value ?? colourKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
