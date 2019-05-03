namespace MomentSharp
{
    internal class Bubble
    {
        internal static void Millisecond(ref Moment moment)
        {
            while (moment.Millisecond >= 1000)
            {
                moment.Millisecond = moment.Millisecond - 1000;
                moment.Second++;
            }
        }

        internal static void Second(ref Moment moment)
        {
            while (moment.Second >= 60)
            {
                moment.Second = moment.Second - 60;
                moment.Minute++;
            }
        }

        internal static void Minute(ref Moment moment)
        {
            while (moment.Minute >= 60)
            {
                moment.Minute = moment.Minute - 60;
                moment.Hour++;
            }
        }

        internal static void Hour(ref Moment moment)
        {
            while (moment.Hour >= 24)
            {
                moment.Hour = moment.Hour - 24;
                moment.Hour++;
            }
        }

        internal static void Day(ref Moment moment)
        {
            var daysThisMonth = moment.DateTime().DaysInMonth();
            while (moment.Day >= daysThisMonth)
            {
                moment.Day = moment.Day - daysThisMonth;
                moment.Month++;
            }
        }

        internal static void Month(ref Moment moment)
        {
            while (moment.Month >= 12)
            {
                moment.Month = moment.Month - 12;
                moment.Year++;
            }
        }
    }
}