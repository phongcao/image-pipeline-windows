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
        ///
        /// @return the current time in milliseconds.
        /// </summary>
        public abstract long Now { get; }
    }
}
