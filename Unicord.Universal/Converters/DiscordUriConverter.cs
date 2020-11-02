using System;
using DSharpPlus.Entities;
using DSharpPlus.Net;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    public class DiscordUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is DiscordUri u ? u.ToUri() : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
