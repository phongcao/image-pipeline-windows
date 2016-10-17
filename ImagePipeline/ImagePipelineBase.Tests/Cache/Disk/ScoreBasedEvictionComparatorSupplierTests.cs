using Cache.Disk;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Test for the score-based eviction comparator.
    /// </summary>
    [TestClass]
    public class ScoreBasedEvictionComparatorSupplierTests
    {
        private static readonly int RANDOM_SEED = 42;
        private static Random _random = new Random(RANDOM_SEED);

        private List<IEntry> _entries;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _entries = new List<IEntry>();
            for (int i = 0; i< 100; i++) 
            {
                MockEntry entry = new MockEntry();
                entry.SetTimeStamp(DateTime.Now.Subtract(TimeSpan.FromMilliseconds(_random.Next())));
                entry.SetSize(_random.Next());
                _entries.Add(entry);
            }
        }

        /// <summary>
        /// Tests the timestamp order
        /// </summary>
        [TestMethod]
        public void TestTimestampOnlyOrder()
        {
            DoTest(1f, 0f);
            for (int i = 0; i < _entries.Count - 1; i++)
            {
                Assert.IsTrue(_entries[i].Timestamp <= _entries[i + 1].Timestamp);
            }
        }

        /// <summary>
        /// Tests the size order
        /// </summary>
        [TestMethod]
        public void TestSizeOnlyOrder()
        {
            DoTest(0f, 1f);
            for (int i = 0; i < _entries.Count - 1; i++)
            {
                Assert.IsTrue(_entries[i].GetSize() >= _entries[i + 1].GetSize());
            }
        }
        /// <summary>
        /// Tests equal
        /// </summary>
        [TestMethod]
        public void TestEqualOrder()
        {
            DoTest(1f, 1f);
        }

        /// <summary>
        /// Tests the weighted order
        /// </summary>
        [TestMethod]
        public void TestWeightedOrder()
        {
            DoTest(2f, 3f);
        }

        private void DoTest(float ageWeight, float sizeWeight)
        {
            ScoreBasedEvictionComparatorSupplier supplier =
                new ScoreBasedEvictionComparatorSupplier(ageWeight, sizeWeight);
            _entries.Sort(supplier.Get());

            for (int i = 0; i < _entries.Count - 1; i++)
            {
                Assert.IsTrue(supplier.CalculateScore(_entries[i], DateTime.Now) >=
                    supplier.CalculateScore(_entries[i + 1], DateTime.Now));
            }
        }
    }
}
