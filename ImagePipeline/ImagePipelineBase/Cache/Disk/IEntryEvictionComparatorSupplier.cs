namespace Cache.Disk
{
    /// <summary>
    /// Provides an instance of eviction comparator.
    /// </summary>
    public interface IEntryEvictionComparatorSupplier
    {
        /// <summary>
        /// Returns the <see cref="IEntryEvictionComparator"/>.
        /// </summary>
        IEntryEvictionComparator Get();
    }
}
