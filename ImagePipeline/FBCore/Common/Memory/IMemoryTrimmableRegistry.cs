namespace FBCore.Common.Memory
{
    /**
     * A class which keeps a list of other classes to be notified of system memory events.
     *
     * <p>If a class uses a lot of memory and needs these notices from the system, it should implement
     * the {@link MemoryTrimmable} interface.
     *
     * <p>Implementations of this class should notify all the trimmables that have registered with it
     * when they need to trim their memory usage.
     */
    public interface IMemoryTrimmableRegistry
    {
        /** Register an object. */
        void RegisterMemoryTrimmable(IMemoryTrimmable trimmable);

        /** Unregister an object. */
        void UnregisterMemoryTrimmable(IMemoryTrimmable trimmable);
    }
}
