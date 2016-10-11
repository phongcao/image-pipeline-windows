using System;

namespace FBCore.Common.Time
{
    /// <summary>
    /// Mock implementation of <see cref="Clock"/>.
    /// </summary>
    public class MockSystemClock : Clock
    {
        private static readonly MockSystemClock INSTANCE = new MockSystemClock();
        private long _now;

        private MockSystemClock()
        {
            _now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static MockSystemClock Get()
        {
            return INSTANCE;
        }

        /// <summary>
        /// Sets the mock datetime in milliseconds.
        /// </summary>
        /// <param name="dateTime"></param>
        public void SetDateTime(long dateTime)
        {
            _now = dateTime;
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
                return _now;
            }
        }
    }
}
