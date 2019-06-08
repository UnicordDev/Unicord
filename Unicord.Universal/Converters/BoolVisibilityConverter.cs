using System;
using System.Collections;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return Visibility.Collapsed;
            }

            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is int i)
            {
                return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is IEnumerable e)
            {
                return e.OfType<object>().Any() ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    class InverseBoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return Visibility.Visible;
            }

            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is int i)
            {
                return i > 0 ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is IEnumerable e)
            {
                return e.OfType<object>().Any() ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
