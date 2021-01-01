using System;
using DSharpPlus.Entities;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Converters
{
    class ColourBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch (value)
            {
                case DiscordColor colour:
                    if (colour.Value != 0)
                        return new SolidColorBrush(Color.FromArgb(255, colour.R, colour.G, colour.B));
                    break;
                case Optional<DiscordColor> optionalColor:
                    if (optionalColor.HasValue)
                        return new SolidColorBrush(Color.FromArgb(255, optionalColor.Value.R, optionalColor.Value.G, optionalColor.Value.B));
                    break;
                case Brush brush:
                    return brush;
                default:
                    break;
            }

            return parameter as Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
