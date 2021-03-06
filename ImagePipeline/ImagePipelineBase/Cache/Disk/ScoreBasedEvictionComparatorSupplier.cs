﻿using System;

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
        /// Instantiates the <see cref="ScoreBasedEvictionComparatorSupplier"/>.
        /// </summary>
        public ScoreBasedEvictionComparatorSupplier(float ageWeight, float sizeWeight)
        {
            _ageWeight = ageWeight;
            _sizeWeight = sizeWeight;
        }

        /// <summary>
        /// Returns the <see cref="IEntryEvictionComparator"/>.
        /// </summary>
        public IEntryEvictionComparator Get()
        {
            return new EntryEvictionComparatorImpl((lhs, rhs) =>
            {
                DateTime now = DateTime.Now;
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
        internal float CalculateScore(IEntry entry, DateTime now)
        {
            long ageMs = Math.Abs((long)(now - entry.Timestamp).TotalMilliseconds);
            long bytes = entry.GetSize();
            return _ageWeight * ageMs + _sizeWeight * bytes;
        }
    }
}
