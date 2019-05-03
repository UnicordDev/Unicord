using System;

namespace MomentSharp
{
    /// <summary>
    /// Emulates http://momentjs.com/docs/#/query/
    /// </summary>
    public static class Query
    {
        /// <summary>
        ///     Check if this DateTime is before <paramref name="compareDateTime"></paramref>, optionally at
        ///     <paramref name="part"></paramref>
        /// </summary>
        /// <param name="dateTime">this DateTime</param>
        /// <param name="compareDateTime">Compare Date</param>
        /// <param name="part"><see cref="DateTimeParts"/></param>
        /// <returns>boolean</returns>
        /// <example>10/20/2010 isBefore 12/31/2010, DateTimeParts.Year = false</example>
        /// <example>10/20/2010 isBefore 01/01/2011, DateTimeParts.Year = true</example>
        public static bool IsBefore(this DateTime dateTime, DateTime compareDateTime,
            DateTimeParts part = DateTimeParts.None)
        {
            if (part == DateTimeParts.None) return dateTime < compareDateTime;

            return dateTime.EndOf(part) < compareDateTime;
        }

        /// <summary>
        ///     Check if this DateTime is after <paramref name="compareDateTime"></paramref>, optionally at
        ///     <paramref name="part"></paramref>
        /// </summary>
        /// <param name="dateTime">this DateTime</param>
        /// <param name="compareDateTime">date to compare</param>
        /// <param name="part"><see cref="DateTimeParts"/></param>
        /// <returns>boolean</returns>
        public static bool IsAfter(this DateTime dateTime, DateTime compareDateTime,
            DateTimeParts part = DateTimeParts.None)
        {
            if (part == DateTimeParts.None) return dateTime > compareDateTime;

            return compareDateTime < dateTime.StartOf(part);
        }

        /// <summary>
        ///     Check if this DateTime is the same as <paramref name="compareDateTime"></paramref>, optionally at
        ///     <paramref name="part"></paramref>
        /// </summary>
        /// <param name="dateTime">this DateTime</param>
        /// <param name="compareDateTime">date to compare</param>
        /// <param name="part"><see cref="DateTimeParts"/></param>
        /// <returns>boolean</returns>
        public static bool IsSame(this DateTime dateTime, DateTime compareDateTime,
            DateTimeParts part = DateTimeParts.None)
        {
            if (part == DateTimeParts.None) return dateTime == compareDateTime;

            return dateTime.StartOf(part) <= compareDateTime && compareDateTime <= dateTime.EndOf(part);
        }

        /// <summary>
        ///     Check if this DateTime is between <paramref name="fromDate"></paramref> and <paramref name="toDate"></paramref>,
        ///     optionally at <paramref name="part"></paramref>
        /// </summary>
        /// <param name="dateTime">this DateTime</param>
        /// <param name="fromDate">Start Date</param>
        /// <param name="toDate">End Date</param>
        /// <param name="part"><see cref="DateTimeParts"/></param>
        /// <returns>boolean</returns>
        public static bool IsBetween(this DateTime dateTime, DateTime fromDate, DateTime toDate,
            DateTimeParts part = DateTimeParts.None)
        {
            return dateTime.IsAfter(fromDate, part) && dateTime.IsBefore(toDate, part);
        }
    }
}