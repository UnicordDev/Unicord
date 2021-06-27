using System;
using Humanizer;
using MomentSharp;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var setting = App.RoamingSettings.Read(Constants.TIMESTAMP_STYLE, TimestampStyle.Absolute);

            if (value is not DateTime t)
            {
                t = value is DateTimeOffset dto ? dto.LocalDateTime : default;
            }

            t = t.ToUniversalTime();

            var moment = new Moment(t);
            if (t != default)
            {
                switch (setting)
                {
                    case TimestampStyle.Relative:
                        return t.Humanize(true);
                    case TimestampStyle.Absolute:
                        return moment.Calendar();
                    case TimestampStyle.Both:
                        return $"{t.Humanize(true)} - {moment.Calendar()}";
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
