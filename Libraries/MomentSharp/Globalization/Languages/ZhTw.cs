using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MomentSharp.Globalization.Languages
{
    /// <summary>
    ///  Localization for traditional chinese (Zh-Tw)
    /// </summary>
    public class ZhTw : ILocalize
    {
        /// <summary>
        /// Chinese locazation implementation constructor
        /// </summary>
        public ZhTw()
        {
            LongDateFormat = new LongDateFormat
            {
                Lt = "HH:mm",
                Lts = "HH:mm:ss",
                L = "yyyy-MM-dd",
                LL = "yyyy年MMMdd日"
            };

            LongDateFormat.LLL = string.Format("yyyy年MMMdd日 {0}", LongDateFormat.Lt);
            LongDateFormat.LLLL = string.Format("yyyy年MMMdd日 dddd {0}", LongDateFormat.Lt);
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
                    return string.Format("今天 {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.NextDay:
                    return string.Format("明天 {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.NextWeek:
                    return string.Format("{0} {1}", dateTime.ToString("dddd"), dateTime.ToString(LongDateFormat.Lt));
                case Calendar.LastDay:
                    return string.Format("昨天 {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.LastWeek:
                    return string.Format("{0} {1}", dateTime.ToString("dddd"), dateTime.ToString(LongDateFormat.Lt));
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
                    results = "幾秒";
                    break;
                case RelativeTime.Minute:
                    results = "1 分鐘";
                    break;
                case RelativeTime.Minutes:
                    results = string.Format("{0} 分鐘", number);
                    break;
                case RelativeTime.Hour:
                    results = "1 小時";
                    break;
                case RelativeTime.Hours:
                    results = string.Format("{0} 小時", number);
                    break;
                case RelativeTime.Day:
                    results = "1 天";
                    break;
                case RelativeTime.Days:
                    results = string.Format("{0} 天", number);
                    break;
                case RelativeTime.Month:
                    results = "1 個月";
                    break;
                case RelativeTime.Months:
                    results = string.Format("{0} 個月", number);
                    break;
                case RelativeTime.Year:
                    results = "1 年";
                    break;
                case RelativeTime.Years:
                    results = string.Format("{0} 年", number);
                    break;
            }
            return !showSuffix ? results : string.Format(isFuture ? "{0}内" : "{0}前", results);
        }
    }
}
