using System;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    public class UriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string s && Uri.TryCreate(s, UriKind.Absolute, out var uri))
            {
                return uri;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
