using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
#else
using System.Windows.Data;

namespace Unicord.Desktop.Converters
{
#endif

    class BoolOpacityConverter : IValueConverter
    {
#if WINDOWS_UWP
        public object Convert(object value, Type targetType, object parameter, string language)
#else
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
#endif
        {
            if (value == null)
                return 0.5;

            if (value is bool b && b)
            {
                return 1;
            }
            else
            {
                return 0.5;
            }
        }
#if WINDOWS_UWP
        public object ConvertBack(object value, Type targetType, object parameter, string language)
#else
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
#endif
        {
            throw new NotImplementedException();
        }

    }
}
