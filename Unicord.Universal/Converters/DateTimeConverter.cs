using Humanizer;
using MomentSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var setting = App.RoamingSettings.Read("TimestampStyle", TimestampStyle.Absolute);

            if (!(value is DateTime t))
            {
                t = default;

                if (value is DateTimeOffset offset)
                {
                    t = offset.UtcDateTime;
                }
            }

            t = t.ToUniversalTime();
            var moment = new Moment(t);

            if (t != default)
            {
                switch (setting)
                {
                    case TimestampStyle.Relative:
                        return t.Humanize();
                    case TimestampStyle.Absolute:
                        return moment.Calendar();
                    case TimestampStyle.Both:
                        return $"{t.Humanize()} - {moment.Calendar()}";
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
