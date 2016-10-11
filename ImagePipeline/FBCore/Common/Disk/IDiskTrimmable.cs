namespace FBCore.Common.Disk
{
    /// <summary>
    /// Any class that uses a lot of disk space and should implement this interface.
    /// </summary>
    public interface IDiskTrimmable
    {
        /// <summary>
        /// Called when there is very little disk space left.
        /// </summary>
        void TrimToMinimum();

        /// <summary>
        /// Called when there is almost no disk space left and the app is likely to crash soon
        /// </summary>
        void TrimToNothing();
    }
}
