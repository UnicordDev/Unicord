namespace MomentSharp.Globalization
{
    /// <summary>
    ///     Calendar parts.
    ///     Meant to emulate http://momentjs.com/docs/#/displaying/calendar-time/
    /// </summary>
    public enum Calendar
    {
        /// <summary>
        /// Text to display if both dates are on the same day, e.g. Today
        /// </summary>
        SameDay,
        /// <summary>
        /// Text to display if orginal date is tomorrow compared to referenceTime
        /// </summary>
        NextDay,
        /// <summary>
        /// Text to display if orginal date is tomorrow compared to referenceTime
        /// </summary>
        NextWeek,
        /// <summary>
        /// Text to display if orginal date is next week compared to referenceTime
        /// </summary>
        LastDay,
        /// <summary>
        /// Text to display if orginal date is yesterday compared to referenceTime
        /// </summary>
        LastWeek,
        /// <summary>
        /// Text to display if orginal date is last week compared to referenceTime
        /// </summary>
        SameElse
    }
}