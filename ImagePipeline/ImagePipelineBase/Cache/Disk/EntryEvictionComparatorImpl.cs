using System;

namespace Cache.Disk
{
    /// <summary>
    /// Provides custom implementation for <see cref="IEntryEvictionComparator"/>.
    /// </summary>
    public class EntryEvictionComparatorImpl : IEntryEvictionComparator
    {
        private Func<IEntry, IEntry, int> _func;

        /// <summary>
        /// Instantiates the <see cref="EntryEvictionComparatorImpl"/>.
        /// </summary>
        public EntryEvictionComparatorImpl(Func<IEntry, IEntry, int> func)
        {
            _func = func;
        }

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is
        /// less than, equal to, or greater than the other.
        /// </summary>
        public int Compare(IEntry x, IEntry y)
        {
            return _func(x, y);
        }
    }
}
