using System;
using Humanizer;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    public class TitleCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string s)
            {
                return s.Humanize(LetterCasing.AllCaps);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
