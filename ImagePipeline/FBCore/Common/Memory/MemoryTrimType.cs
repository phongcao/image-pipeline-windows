namespace FBCore.Common.Memory
{
    /**
     * Types of memory trim.
     *
     * <p>Each type of trim will provide a suggested trim ratio.
     *
     * <p>A {@link MemoryTrimmableRegistry} implementation sends out memory trim events with this type.
     */
    public struct MemoryTrimType
    {
        /** The application is approaching the device-specific Java heap limit. */
        public const double OnCloseToDalvikHeapLimit = 0.5;

        /** The system as a whole is running out of memory, and this application is in the foreground. */
        public const double OnSystemLowMemoryWhileAppInForeground = 0.5;

        /** The system as a whole is running out of memory, and this application is in the background. */
        public const double OnSystemLowMemoryWhileAppInBackground = 1;

        /** This app is moving into the background, usually because the user navigated to another app. */
        public const double OnAppBackgrounded = 1;
    }
}
