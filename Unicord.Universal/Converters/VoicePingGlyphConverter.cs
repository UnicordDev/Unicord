using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class VoicePingGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var ping = (uint)value;
            if(ping < 150)
            {
                return "\xE908";
            }

            if (ping < 250)
            {
                return "\xE907";
            }

            if (ping < 350)
            {
                return "\xE906";
            }

            if (ping < 450)
            {
                return "\xE905";
            }

            return "\xE904";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
