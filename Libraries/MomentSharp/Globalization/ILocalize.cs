using System;
using System.Diagnostics.CodeAnalysis;

namespace MomentSharp.Globalization
{
    /// <summary>
    ///     Base class for implementing language translations.
    /// </summary>
    public interface ILocalize
    {
        /// <summary>
        /// Localized short hand format strings. See http://momentjs.com/docs/#localized-formats
        /// </summary>
        LongDateFormat LongDateFormat { get; }
        /// <summary>
        /// Localized <see cref="Calendar"/> parts for <paramref name="dateTime"/>
        /// </summary>
        /// <param name="calendar">Calendar Part</param>
        /// <param name="dateTime">DateTime to use in format string</param>
        /// <returns>Localized string e.g. Today at 9:00am</returns>
        string Translate(Calendar calendar, DateTime dateTime);
        /// <summary>
        /// Localize <see cref="RelativeTime"/>. This is meant to emulate how MomentJs allows localization of RelativeTime
        /// </summary>
        /// <param name="relativeTime"><see cref="RelativeTime"/></param>
        /// <param name="number">Difference amount</param>
        /// <param name="showSuffix">Should suffix? e.g. "ago"</param>
        /// <param name="isFuture">Difference is in the future or not. e.g. Yesterday vs Tomorrow</param>
        /// <returns>Localized realtive time e.g.: 5 seconds ago</returns>
        string Translate(RelativeTime relativeTime, int number, bool showSuffix, bool isFuture);
    }

    /// <summary>
    ///     Extra for formats from Momentjs. Some of these are may already exist in DateTime.ToString(*) See http://momentjs.com/docs/#localized-formats
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LongDateFormat
    {
        /// <summary>
        /// Time: 8:30 PM
        /// </summary>
        public string Lt { get; set; }

        /// <summary>
        /// Time with seconds: 8:30:25 PM
        /// </summary>
        public string Lts { get; set; }

        /// <summary>
        /// Month numeral, day of month, year: 09/04/1986
        /// </summary>
        public string L { get; set; }

        /// <summary>
        /// Month numeral, day of month, year: 9/4/1986
        /// </summary>
        public string l { get; set; }

        /// <summary>
        /// Month name, day of month, year:	September 4 1986
        /// </summary>
        public string LL { get; set; }

        /// <summary>
        /// Month name, day of month, year:	Sep 4 1986
        /// </summary>
        public string ll { get; set; }

        /// <summary>
        /// Month name, day of month, year, time: September 4 1986 8:30 PM
        /// </summary>
        public string LLL { get; set; }

        /// <summary>
        /// Month name, day of month, year, time: Sep 4 1986 8:30 PM
        /// </summary>
        public string lll { get; set; }

        /// <summary>
        /// Month name, day of month, day of week, year, time: Thursday, September 4 1986 8:30 PM
        /// </summary>
        public string LLLL { get; set; }

        /// <summary>
        /// Month name, day of month, day of week, year, time: Thu, Sep 4 1986 8:30 PM
        /// </summary>
        public string llll { get; set; }
    }
}