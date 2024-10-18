using System;
using Humanizer;
using MomentSharp;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class DateTimeConverter : IValueConverter
    {
        public static string Convert(TimestampStyle style, DateTime dt)
        {
            dt = dt.ToUniversalTime();

            switch (style)
            {
                case TimestampStyle.Relative:
                    return dt.Humanize(true);
                case TimestampStyle.Absolute:
                    return new Moment(dt).Calendar();
                case TimestampStyle.Both:
                    return $"{dt.Humanize(true)} - {new Moment(dt).Calendar()}";
                default:
                    return dt.ToShortDateString();
            }
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is not TimestampStyle style)
                style = (TimestampStyle)App.RoamingSettings.Read(Constants.TIMESTAMP_STYLE, (int)TimestampStyle.Absolute);

            if (value is not DateTime dt)
                dt = ((DateTimeOffset)value).DateTime;

            return Convert(style, dt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}