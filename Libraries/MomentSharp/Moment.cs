using System;
using System.Globalization;
using System.Threading;
using MomentSharp.Globalization;
using MomentSharp.Globalization.Languages;

namespace MomentSharp
{
    /// <summary>
    /// Moment object which provides support for several DateTime functions that are not built-in to C#
    /// </summary>
    public struct Moment
    {
        /// <summary>
        ///     Get's a new Moment defaulting values to DateTime.UtcNow, unless <paramref name="zero" /> is true in which values
        ///     will be set to the min value
        /// </summary>
        /// <param name="zero">use min values instead of UtcNow</param>
        public Moment(DateTime? now = null)
        {
            if (now != null)
            {
                Year = now.Value.Year;
                Month = now.Value.Month;
                Day = now.Value.Day;
                Hour = now.Value.Hour;
                Minute = now.Value.Minute;
                Second = now.Value.Second;
                Millisecond = now.Value.Millisecond;
            }
            else
            {
                Year = DateTime.MinValue.Year;
                Month = 1;
                Day = 1;
                Hour = 0;
                Minute = 0;
                Second = 0;
                Millisecond = 0;
            }
            Language = SetLanguageByCulture();
        }

        /// <summary>
        /// Date's Year
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Date's Month
        /// </summary>
        public int Month { get; set; }
        
        /// <summary>
        /// Date's Day
        /// </summary>
        public int Day { get; set; }

        /// <summary>
        /// Date's Hour
        /// </summary>
        public int Hour { get; set; }

        /// <summary>
        /// Date's Minute
        /// </summary>
        public int Minute { get; set; }

        /// <summary>
        /// Date's Second
        /// </summary>
        public int Second { get; set; }

        /// <summary>
        /// Date's Millisecond
        /// </summary>
        public int Millisecond { get; set; }

        /// <summary>
        /// Local/Language to use
        /// </summary>
        public ILocalize Language { get; set; }

        /// <summary>
        /// Attempts to find the correct <see cref="ILocalize"/> based on the <see cref="Thread.CurrentThread"/> CurrentCulture
        /// </summary>
        /// <returns>ILocalize</returns>
        private static ILocalize SetLanguageByCulture()
        {
            var culture = CultureInfo.CurrentUICulture.ToString().Replace("-", "");
            switch (culture)
            {
                case "enUS":
                    return new EnUs();
                case "de":
                    return new De();
                case "zhCN":
                    return new ZhCn();
                case "zhTW":
                    return new ZhTw();
            }
            return new EnUs();
        }
    }
}