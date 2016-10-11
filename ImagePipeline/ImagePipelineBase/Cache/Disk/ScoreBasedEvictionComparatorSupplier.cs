using System;
using System.Collections.Generic;

namespace Cache.Disk
{
    /// <summary>
    /// Evicts cache items based on a mix of their size and timestamp.
    /// </summary>
    public class ScoreBasedEvictionComparatorSupplier : IEntryEvictionComparatorSupplier
    {
        private readonly float _ageWeight;
        private readonly float _sizeWeight;

        /// <summary>
        /// Instantiates the <see cref="ScoreBasedEvictionComparatorSupplier"/>
        /// </summary>
        /// <param name="ageWeight"></param>
        /// <param name="sizeWeight"></param>
        public ScoreBasedEvictionComparatorSupplier(float ageWeight, float sizeWeight)
        {
            _ageWeight = ageWeight;
            _sizeWeight = sizeWeight;
        }

        /// <summary>
        /// Returns the <see cref="IEntryEvictionComparator"/>
        /// </summary>
        /// <returns></returns>
        public IEntryEvictionComparator Get()
        {
            return new EntryEvictionComparatorHelper((lhs, rhs) =>
            {
                long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                float score1 = CalculateScore(lhs, now);
                float score2 = CalculateScore(rhs, now);
                return score1 < score2 ? 1 : ((score2 == score1) ? 0 : -1);
            });
        }

        /// <summary>
        /// Calculates an eviction score.
        ///
        /// Entries with a higher eviction score should be evicted first.
        /// </summary>
        internal float CalculateScore(IEntry entry, long now)
        {
            long ageMs = now - entry.Timestamp;
            long bytes = entry.GetSize();
            return _ageWeight * ageMs + _sizeWeight * bytes;
        }
    }
}
