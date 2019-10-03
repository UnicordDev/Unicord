using System;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var val = value is null ? false : value is bool b ? b : value is int i ? i > 0 ? true : false : true;

            if(parameter != null)
            {
                return !val;
            }

            return val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
