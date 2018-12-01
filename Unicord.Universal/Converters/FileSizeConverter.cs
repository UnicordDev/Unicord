using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int i)
            {
                return Files.SizeSuffix(i);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
