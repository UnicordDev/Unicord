using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Converters
{
    class ColourBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DiscordColor colour && colour.Value != default(DiscordColor).Value)
            {
                return new SolidColorBrush(Color.FromArgb(255, colour.R, colour.G, colour.B));
            }
            else if (value is Brush)
            {
                return value;
            }
            else if (parameter is Brush)
            {
                return parameter as Brush;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
