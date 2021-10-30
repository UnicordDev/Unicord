using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    public class AttachmentTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var type = ((AttachmentType)value);
            var specified = (string)parameter;

            return Enum.GetName(typeof(AttachmentType), type) == specified;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
