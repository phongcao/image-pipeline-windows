using System.Collections.Generic;

namespace Cache.Disk
{
    /// <summary>
    /// Defines an order the items are being evicted from the cache.
    /// </summary>
    public interface IEntryEvictionComparator : IComparer<IEntry>
    {
    }
}
