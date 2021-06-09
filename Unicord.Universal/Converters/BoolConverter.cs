using System;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is not null && (value is bool b ? b : !(value is int i && i <= 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
