using System;

namespace FBCore.Common.Time
{
    /// <summary>
    /// Implementation of <see cref="Clock"/> that delegates to the system clock.
    /// </summary>
    public class SystemClock : Clock
    {
        private static readonly SystemClock INSTANCE = new SystemClock();
        private static DateTime JAN_1ST_1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private SystemClock()
        {
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static SystemClock Get()
        {
            return INSTANCE;
        }

        /// <summary>
        /// Gets the current time in milliseconds.
        ///
        /// @return the current time in milliseconds.
        /// </summary>
        public override long CurrentTimeMillis
        {
            get
            {
                return (long)((DateTime.UtcNow - JAN_1ST_1970).TotalMilliseconds);
            }
        }

        /// <summary>
        /// Gets the current time.
        ///
        /// @return the current time.
        /// </summary>
        public override DateTime Now
        {
            get
            {
                return DateTime.Now;
            }
        }
    }
}
