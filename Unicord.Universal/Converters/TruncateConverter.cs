using System;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class TruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                return str.Length > 384 ? str.Substring(0, 381) + "..." : str;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
