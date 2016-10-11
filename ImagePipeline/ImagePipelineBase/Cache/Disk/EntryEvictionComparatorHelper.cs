using System;

namespace Cache.Disk
{
    /// <summary>
    /// Helper class for <see cref="IEntryEvictionComparator"/>
    /// </summary>
    public class EntryEvictionComparatorHelper : IEntryEvictionComparator
    {
        private Func<IEntry, IEntry, int> _func;

        /// <summary>
        ///  Instantiates the <see cref="EntryEvictionComparatorHelper"/>
        /// </summary>
        /// <param name="func"></param>
        public EntryEvictionComparatorHelper(Func<IEntry, IEntry, int> func)
        {
            _func = func;
        }

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than,
        /// equal to, or greater than the other.
        /// </summary>
        public int Compare(IEntry x, IEntry y)
        {
            return _func(x, y);
        }
    }
}
