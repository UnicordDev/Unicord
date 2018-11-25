using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using WamWooWam.Wpf.Tools;

namespace WamWooWam.Wpf.Converters
{
    class LightenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var offset = 0.2f;

            if (parameter is float f || float.TryParse(parameter.ToString(), out f))
            {
                offset = f;
            }

            if (value is Color c)
            {
                return c.Lighten(offset);
            }
            else if (value is SolidColorBrush b)
            {
                var col = b.Color.Lighten(offset);
                return new SolidColorBrush(col);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
