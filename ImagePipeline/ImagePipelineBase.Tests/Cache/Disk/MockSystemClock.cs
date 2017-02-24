using FBCore.Common.Time;
using System;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Mock implementation of <see cref="Clock"/>.
    /// </summary>
    class MockSystemClock : Clock
    {
        private static readonly MockSystemClock INSTANCE = new MockSystemClock();
        private DateTime _now;

        private MockSystemClock()
        {
            _now = DateTime.Now;
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
        public void SetDateTime(DateTime dateTime)
        {
            _now = dateTime;
        }

        /// <summary>
        /// Gets the current time in milliseconds.
        ///
        /// <returns>the current time in milliseconds.</returns>
        /// </summary>
        public override long CurrentTimeMillis
        {
            get
            {
                return (_now.Ticks / TimeSpan.TicksPerMillisecond);
            }
        }

        /// <summary>
        /// Gets the current time in milliseconds.
        ///
        /// <returns>the current time in milliseconds.</returns>
        /// </summary>
        public override DateTime Now
        {
            get
            {
                return _now;
            }
        }
    }
}
