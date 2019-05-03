using System;

namespace MomentSharp.Globalization.Languages
{
    /// <summary>
    /// Localization for German (De)
    /// </summary>
    public class De : ILocalize
    {
        /// <summary>
        /// German locazation implementation constructor
        /// </summary>
        public De()
        {
            LongDateFormat = new LongDateFormat
            {
                Lt = "HH:mm",
                Lts = "HH:mm:ss",
                L = "dd.MM.yyyy",
                LL = "d. MMMM yyyy"
            };

            LongDateFormat.LLL = string.Format("d. MMMM yyyy {0}", LongDateFormat.Lt);
            LongDateFormat.LLLL = string.Format("dddd, d. MMMM yyyy {0}", LongDateFormat.Lt);
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
                    return string.Format("Heute um {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.NextDay:
                    return string.Format("Morgen um {0}", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.NextWeek:
                    return string.Format("{0} um {1} Uhr", dateTime.ToString("dddd"),
                        dateTime.ToString(LongDateFormat.Lt));
                case Calendar.LastDay:
                    return string.Format("Gestern um {0} Uhr", dateTime.ToString(LongDateFormat.Lt));
                case Calendar.LastWeek:
                    return string.Format("letzten {0} um {1} Uhr", dateTime.ToString("dddd"),
                        dateTime.ToString(LongDateFormat.Lt));
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
                    results = "ein paar Sekunden";
                    break;
                case RelativeTime.Minute:
                    results = showSuffix ? "einer Minute" : "eine Minute";
                    break;
                case RelativeTime.Minutes:
                    results = string.Format("{0} Minuten", number);
                    break;
                case RelativeTime.Hour:
                    results = showSuffix ? "einer Stunde" : "eine Stunde";
                    break;
                case RelativeTime.Hours:
                    results = string.Format("{0} Stunden", number);
                    break;
                case RelativeTime.Day:
                    results = showSuffix ? "einem Tag" : "ein Tag";
                    break;
                case RelativeTime.Days:
                    results = string.Format("{0} {1}", number, showSuffix ? "Tagen" : "Tage");
                    break;
                case RelativeTime.Month:
                    results = showSuffix ? "einem Monat" : "ein Monat";
                    break;
                case RelativeTime.Months:
                    results = string.Format("{0} {1}", number, showSuffix ? "Monaten" : "Monate");
                    break;
                case RelativeTime.Year:
                    results = showSuffix ? "einem Jahr" : "ein Jahr";
                    break;
                case RelativeTime.Years:
                    results = string.Format("{0} {1}", number, showSuffix ? "Jahren" : "Jahre");
                    break;
            }
            return !showSuffix ? results : string.Format(isFuture ? "in {0}" : "vor {0}", results);
        }
    }
}