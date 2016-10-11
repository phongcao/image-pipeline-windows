using Cache.Disk;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Test for <see cref="DefaultEntryEvictionComparatorSupplier"/>
    /// </summary>
    [TestClass]
    public class DefaultEntryEvictionComparatorSupplierTests
    {
        private static readonly int RANDOM_SEED = 42;

        /// <summary>
        /// Tests the sorting order of the entries
        /// </summary>
        [TestMethod]
        public void TestSortingOrder()
        {
            Random random = new Random(RANDOM_SEED);
            List<IEntry> entries = new List<IEntry>();
            for (int i = 0; i < 100; i++)
            {
                byte[] buffer = new byte[8];
                random.NextBytes(buffer);
                entries.Add(CreateEntry(BitConverter.ToInt64(buffer, 0)));
            }

            entries.Sort(new DefaultEntryEvictionComparatorSupplier().Get());

            for (int i = 0; i < entries.Count - 1; i++)
            {
                Assert.IsTrue(entries[i].Timestamp < entries[i + 1].Timestamp);
            }
        }

        private static IEntry CreateEntry(long time)
        {
            MockEntry entry = new MockEntry();
            entry.SetTimeStamp(time);
            return entry;
        }
    }
}
