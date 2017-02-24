using System;

namespace FBCore.Common.Time
{
    /// <summary>
    /// Abstract class for getting the current time.
    /// </summary>
    public abstract class Clock
    {
        /// <summary>
        /// The maximum time.
        /// </summary>
        public long MAX_TIME = long.MaxValue;

        /// <summary>
        /// Gets the current time in milliseconds.
        /// </summary>
        /// <returns>The current time in milliseconds.</returns>
        public abstract long CurrentTimeMillis { get; }

        /// <summary>
        /// Gets the current time.
        /// </summary>
        /// <returns>The current time in milliseconds.</returns>
        public abstract DateTime Now { get; }
    }
}
