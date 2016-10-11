using System;

namespace FBCore.Common.Time
{
    /// <summary>
    /// Implementation of <see cref="Clock"/> that delegates to the system clock.
    /// </summary>
    public class SystemClock : Clock
    {
        private static readonly SystemClock INSTANCE = new SystemClock();

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
        public override long Now
        {
            get
            {
                return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }
    }
}
