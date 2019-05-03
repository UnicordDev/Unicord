using System;

namespace MomentSharp.Globalization.Languages
{
    /// <summary>
    ///  Localization for English (US,American) (En-US)
    /// </summary>
    public class EnUs : ILocalize
    {
        /// <summary>
        /// English locazation implementation constructor
        /// </summary>
        public EnUs()
        {
            LongDateFormat = new LongDateFormat
            {
                Lt = "h:mm tt",
                Lts = "h:mm:s tt",
                L = "M/d/yyyy",
                LL = "MMMM d, yyyy"
            };

            LongDateFormat.LLL = string.Format("MMMM d, yyyy {0}", LongDateFormat.Lt);
            LongDateFormat.LLLL = string.Format("dddd, MMMM d, yyyy {0}", LongDateFormat.Lt);
        }

        /// <summary>
        /// Localized short hand format strings. See http://momentjs.com/docs/#localized-formats
        /// </summary>
        public LongDateFormat LongDateFormat { get; set; }


        /// <summary>
        /// Localized <see cref="Calendar"/> parts for <paramref name="dateTime"/>
        /// </summary>
        /// <param name="calendar">Calendar Part</param>
        /// <param name="dateTime">DateTime to use in format string</param>
        /// <returns>Localized string e.g. Today at 9:00am</returns>
        public string Translate(Calendar calendar, DateTime dateTime)
        {
            switch (calendar)
            {
                case Calendar.SameDay:
                    return string.Format("Today at {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.NextDay:
                    return string.Format("Tomorrow at {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.NextWeek:
                    return string.Format("{0} at {1}", dateTime.ToString("dddd"), dateTime.ToString(LongDateFormat.Lt));
                case Calendar.LastDay:
                    return string.Format("Yesterday at {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.LastWeek:
                    return string.Format("{0} at {1}", dateTime.ToString("dddd"), dateTime.ToString(LongDateFormat.Lt));
                case Calendar.SameElse:
                    return dateTime.ToString(LongDateFormat.L);
            }
            return "";
        }


        /// <summary>
        /// Localize <see cref="RelativeTime"/>. This is meant to emulate how MomentJs allows localization of RelativeTime
        /// </summary>
        /// <param name="relativeTime"><see cref="RelativeTime"/></param>
        /// <param name="number">Difference amount</param>
        /// <param name="showSuffix">Should suffix? e.g. "ago"</param>
        /// <param name="isFuture">Difference is in the future or not. e.g. Yesterday vs Tomorrow</param>
        /// <returns>Localized realtive time e.g.: 5 seconds ago</returns>
        public string Translate(RelativeTime relativeTime, int number, bool showSuffix, bool isFuture)
        {
            var results = string.Empty;
            switch (relativeTime)
            {
                case RelativeTime.Seconds:
                    results = "a few seconds";
                    break;
                case RelativeTime.Minute:
                    results = "a minute";
                    break;
                case RelativeTime.Minutes:
                    results = string.Format("{0} minutes", number);
                    break;
                case RelativeTime.Hour:
                    results = "an hour";
                    break;
                case RelativeTime.Hours:
                    results = string.Format("{0} hours", number);
                    break;
                case RelativeTime.Day:
                    results = "a day";
                    break;
                case RelativeTime.Days:
                    results = string.Format("{0} days", number);
                    break;
                case RelativeTime.Month:
                    results = "a month";
                    break;
                case RelativeTime.Months:
                    results = string.Format("{0} months", number);
                    break;
                case RelativeTime.Year:
                    results = "a year";
                    break;
                case RelativeTime.Years:
                    results = string.Format("{0} years", number);
                    break;
            }
            return !showSuffix ? results : string.Format(isFuture ? "in {0}" : "{0} ago", results);
        }
    }
}