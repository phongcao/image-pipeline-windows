namespace FBCore.Common.Memory
{
    /**
     * A class can implement this interface to react to a {@link MemoryTrimmableRegistry}'s request to
     * trim memory.
     */

    public interface IMemoryTrimmable
    {
        /**
         * Trim memory.
         */
        void Trim(MemoryTrimType trimType);
    }
}
