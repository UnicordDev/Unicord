using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using static Unicord.Constants;
using ColorHelper = Microsoft.Toolkit.Uwp.Helpers.ColorHelper;

namespace Unicord.Universal.Converters
{
    public class ColourBrushConverter : IValueConverter
    {
        private Dictionary<uint, SolidColorBrush> _brushCache
         = new Dictionary<uint, SolidColorBrush>();

        public Color DefaultBackgroundColour { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Brush b)
                return b;

            var col = new DiscordColor();
            if (value is DiscordColor)
                col = (DiscordColor)value;

            if (value is Optional<DiscordColor> opt && opt.HasValue)
                col = opt.Value;

            if (col.Value == 0)
                return null;

            var winCol = Color.FromArgb(255, col.R, col.G, col.B);

            if (!App.RoamingSettings.Read(ADJUST_ROLE_COLOURS, true))
                return GetOrCreateBrush(winCol);

            var hslCol = ColorHelper.ToHsl(winCol);
            var contrast = App.RoamingSettings.Read(MINIMUM_CONTRAST, MINIMUM_CONTRAST_DEFAULT);
            var backgroundCol = ColorHelper.ToHsl(parameter is Color ? (Color)parameter : DefaultBackgroundColour);

            var dark = backgroundCol.L < 0.5;
            var targetLuma = dark ? contrast * (backgroundCol.L + 0.05) - 0.05 : (backgroundCol.L + 0.05) / contrast - 0.05;
            if (dark ? hslCol.L < targetLuma : hslCol.L > targetLuma)
                hslCol.L = targetLuma;

            var targetCol = ColorHelper.FromHsl(hslCol.H, hslCol.S, hslCol.L, hslCol.A);

            return GetOrCreateBrush(winCol, targetCol);
        }

        private SolidColorBrush GetOrCreateBrush(Color colourKey, Color? value = null)
        {
            var argb = (uint)((colourKey.A << 24) | (colourKey.R << 16) | (colourKey.G << 8) | colourKey.B);

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
