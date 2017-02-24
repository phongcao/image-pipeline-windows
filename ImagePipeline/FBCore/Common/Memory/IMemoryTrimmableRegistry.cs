namespace FBCore.Common.Memory
{
    /// <summary>
    /// A class which keeps a list of other classes to be notified of
    /// system memory events.
    ///
    /// <para />If a class uses a lot of memory and needs these notices
    /// from the system, it should implement the <see cref="IMemoryTrimmable"/>
    /// interface.
    ///
    /// <para />Implementations of this class should notify all the trimmables
    /// that have registered with it when they need to trim their memory usage.
    /// </summary>
    public interface IMemoryTrimmableRegistry
    {
        /// <summary>
        /// Register an object.
        /// </summary>
        void RegisterMemoryTrimmable(IMemoryTrimmable trimmable);

        /// <summary>
        /// Unregister an object.
        /// </summary>
        void UnregisterMemoryTrimmable(IMemoryTrimmable trimmable);
    }
}
