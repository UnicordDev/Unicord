using System;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    public class MutedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return 0.5;
            }

            if (value is bool b && b)
            {
                return 0.5;
            }
            else
            {
                return 1.0; // must return double
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
