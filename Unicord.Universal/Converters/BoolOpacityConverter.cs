using System;
using DSharpPlus.Entities;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class BoolOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return 0.5;
            }

            if (value is Optional<bool> op)
                if (op.HasValue && op.Value)
                    return 1.0;
                else
                    return 0.5;

            if (value is bool b && b)
            {
                return 1.0;
            }
            else
            {
                return 0.5;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }
}
