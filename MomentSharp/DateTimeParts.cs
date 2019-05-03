using System;

#pragma warning disable 1591

namespace MomentSharp
{
    /// <summary>
    ///     Used to identify a time part like <see cref="Manipulate.EndOf(DateTime, DateTimeParts)" />
    /// </summary>
    public enum DateTimeParts
    {
        Year,
        Month,
        Quarter,
        Day,
        Hour,
        Minute,
        Second,
        Millisecond,
        Week,

        /// <summary>
        ///     Do NOT use in your code. This is meant to be used in default parameters
        /// </summary>
        None
    }
}