using System;

namespace MomentSharp
{
    /// <summary>
    /// Emulates http://momentjs.com/docs/#/parsing/
    /// </summary>
    public static class Parse
    {
        /// <summary>
        ///     Converts javascript/Unix timestamp to DateTime
        /// </summary>
        /// <param name="unixTimeStamp">TimeStamp in seconds</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime UnixToDateTime(this double unixTimeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dateTime;
        }

        /// <summary>
        ///     Converts javascript/Unix timestamp to DateTime
        /// </summary>
        /// <param name="unixTimeStamp">TimeStamp in seconds</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime UnixToDateTime(this int unixTimeStamp)
        {
            return UnixToDateTime((double) unixTimeStamp);
        }

        /// <summary>
        ///     Convert this <paramref name="moment" /> object to a <see cref="System.DateTime" />
        /// </summary>
        /// <param name="moment">A Moment Object</param>
        /// <param name="bubble">
        ///     Whether or not to bubble <paramref name="moment" /> to the next part. E.g. 90 seconds to 1 minute and 30 seconds.
        ///     If false, will throw exception given the example.
        /// </param>
        /// <returns>DateTime</returns>
        public static DateTime DateTime(this Moment moment, bool bubble = false)
        {
            if (!bubble)
                return new DateTime(moment.Year, moment.Month, moment.Day, moment.Hour, moment.Minute, moment.Second,
                    moment.Millisecond);

            Bubble.Millisecond(ref moment);
            Bubble.Second(ref moment);
            Bubble.Minute(ref moment);
            Bubble.Hour(ref moment);
            Bubble.Day(ref moment);
            Bubble.Month(ref moment);
            return new DateTime(moment.Year, moment.Month, moment.Day, moment.Hour, moment.Minute, moment.Second,
                moment.Millisecond);
        }

        /// <summary>
        /// Convert this <paramref name="moment" /> object to LocalTime <see cref="System.DateTime" />
        /// </summary>
        /// <param name="moment">A Moment Object</param>
        /// <returns>DateTime</returns>
        public static DateTime LocalTime(this Moment moment)
        {
            var dateTime = moment.DateTime();
            return dateTime.ToLocalTime();
        }

        /// <summary>
        ///     Converts this <paramref name="dateTime" /> to a <see cref="Moment" /> object
        /// </summary>
        /// <param name="dateTime">this DateTime</param>
        /// <returns><see cref="Moment"/></returns>
        public static Moment Moment(this DateTime dateTime)
        {
            return new Moment
            {
                Year = dateTime.Year,
                Month = dateTime.Month,
                Day = dateTime.Day,
                Hour = dateTime.Hour,
                Minute = dateTime.Minute,
                Second = dateTime.Second,
                Millisecond = dateTime.Millisecond
            };
        }

        /// <summary>
        ///     Converts this <paramref name="dateTime" /> to UTC
        /// </summary>
        /// <param name="dateTime">this DateTime</param>
        /// <param name="fromTimeZoneId">For valid parameters see TimeZoneInfo.GetSystemTimeZones()</param>
        /// <returns><see cref="DateTime"/></returns>
        public static DateTime ToUTC(this DateTime dateTime, string fromTimeZoneId)
        {
            return dateTime.ToUniversalTime();
        }

        /// <summary>
        ///     Converts this <paramref name="dateTime" /> UTC time to another time zone
        /// </summary>
        /// <param name="dateTime">this DateTime</param>
        /// <param name="toTimeZoneId">For valid parameters see TimeZoneInfo.GetSystemTimeZones()</param>
        /// <returns><see cref="DateTime"/></returns>
        public static DateTime ToTimeZone(this DateTime dateTime, string toTimeZoneId)
        {
            dateTime = System.DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.FindSystemTimeZoneById(toTimeZoneId));
        }
    }
}