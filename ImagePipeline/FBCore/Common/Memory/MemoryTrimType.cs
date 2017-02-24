namespace FBCore.Common.Memory
{
    /// <summary>
    /// Types of memory trim.
    ///
    /// <para />Each type of trim will provide a suggested trim ratio.
    ///
    /// <para />A <see cref="IMemoryTrimmableRegistry"/> implementation sends out
    /// memory trim events with this type.
    /// </summary>
    public struct MemoryTrimType
    {
        /// <summary>
        /// The application is approaching the device-specific Java heap limit.
        /// </summary> 
        public const double OnCloseToDalvikHeapLimit = 0.5;

        /// <summary>
        /// The system as a whole is running out of memory, and this application
        /// is in the foreground.
        /// </summary>
        public const double OnSystemLowMemoryWhileAppInForeground = 0.5;

        /// <summary>
        /// The system as a whole is running out of memory, and this application
        /// is in the background.
        /// </summary>
        public const double OnSystemLowMemoryWhileAppInBackground = 1;

        /// <summary>
        /// This app is moving into the background, usually because the user
        /// navigated to another app.
        /// </summary>
        public const double OnAppBackgrounded = 1;
    }
}
