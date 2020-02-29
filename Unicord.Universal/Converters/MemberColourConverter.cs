using System;
using DSharpPlus.Entities;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Converters
{
    public class MemberColourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // TODO: cache?
            return value is DiscordColor colour && colour.Value != 0 ? new SolidColorBrush(Color.FromArgb(255, colour.R, colour.G, colour.B)) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
