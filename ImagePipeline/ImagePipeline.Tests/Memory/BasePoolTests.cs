using FBCore.Common.Internal;
using ImagePipeline.Memory;
using ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Tests for <see cref="BasePool{T}"/> 
    /// </summary>
    [TestClass]
    public class BasePoolTests
    {
        private TestPool _pool;
        private PoolStats<byte[]> _stats;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _pool = new TestPool(10, 14);
            _stats = new PoolStats<byte[]>(_pool);
        }

        /// <summary>
        /// Test out the Alloc method
        /// </summary>
        [TestMethod]
        public void TestAlloc()
        {
            Assert.AreEqual(1, _pool.Alloc(1).Length);
            Assert.AreEqual(3, _pool.Alloc(3).Length);
            Assert.AreEqual(2, _pool.Alloc(2).Length);
        }

        /// <summary>
        /// Test out the Free method
        /// </summary>
        [TestMethod]
        public void TestFree()
        {
        }

        /// <summary>
        /// Tests out the GetBucketedSize method
        /// </summary>
        [TestMethod]
        public void TestGetBucketedSize()
        {
            Assert.AreEqual(2, _pool.GetBucketedSize(1));
            Assert.AreEqual(2, _pool.GetBucketedSize(2));
            Assert.AreEqual(4, _pool.GetBucketedSize(3));
            Assert.AreEqual(6, _pool.GetBucketedSize(6));
            Assert.AreEqual(8, _pool.GetBucketedSize(7));
        }

        /// <summary>
        /// Tests out the GetBucketedSize method for invalid inputs
        /// </summary>
        [TestMethod]
        public void TestGetBucketedSize_Invalid()
        {
            var sizes = new int[] { -1, 0 };
            foreach (int s in sizes)
            {
                try
                {
                    _pool.GetBucketedSize(s);
                    Assert.Fail("Failed size: " + s);
                }
                catch (InvalidSizeException)
                {
                    // This is expected
                }
            }
        }

        /// <summary>
        /// Tests out the GetBucketedSizeForValue method
        /// </summary>
        [TestMethod]
        public void TestGetBucketedSizeForValue()
        {
            Assert.AreEqual(2, _pool.GetBucketedSizeForValue(new byte[2]));
            Assert.AreEqual(3, _pool.GetBucketedSizeForValue(new byte[3]));
            Assert.AreEqual(6, _pool.GetBucketedSizeForValue(new byte[6]));
        }

        /// <summary>
        /// Tests out the GetSizeInBytes method
        /// </summary>
        [TestMethod]
        public void TestGetSizeInBytes()
        {
            Assert.AreEqual(1, _pool.GetSizeInBytes(1));
            Assert.AreEqual(2, _pool.GetSizeInBytes(2));
            Assert.AreEqual(3, _pool.GetSizeInBytes(3));
            Assert.AreEqual(5, _pool.GetSizeInBytes(5));
            Assert.AreEqual(4, _pool.GetSizeInBytes(4));
        }

        /// <summary>
        /// Get via Alloc
        /// </summary>
        [TestMethod]
        public void TestGet_Alloc()
        {
            // Get a buffer - causes an alloc
            var b1 = _pool.Get(1);
            Assert.IsNotNull(b1);
            Assert.AreEqual(2, b1.Length);
            Assert.IsTrue(_pool.InUseValues.Contains(b1));
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(1, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(2, _stats.UsedBytes);
            Assert.AreEqual(0, _stats.FreeCount);
            Assert.AreEqual(1, _stats.UsedCount);

            // Release this buffer
            _pool.Release(b1);
            Assert.IsFalse(_pool.InUseValues.Contains(b1));

            // Get another buffer, but of a different size. No reuse possible
            var b2 = _pool.Get(3);
            Assert.IsNotNull(b2);
            Assert.AreEqual(4, b2.Length);
            Assert.IsTrue(_pool.InUseValues.Contains(b2));
            _stats.Refresh();
            testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(0, 1) },
                {  4, new Tuple<int, int>(1, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(2, _stats.FreeBytes);
            Assert.AreEqual(4, _stats.UsedBytes);
            Assert.AreEqual(1, _stats.FreeCount);
            Assert.AreEqual(1, _stats.UsedCount);
        }

        /// <summary>
        /// Get via Alloc+Trim
        /// </summary>
        [TestMethod]
        public void TestGet_AllocAndTrim()
        {
            _pool = new TestPool(10, 10, MakeBucketSizeArray(2, 2, 4, 2, 6, 2));
            _stats.SetPool(_pool);

            // Allocate and release multiple buffers
            byte[] b1 = _pool.Get(2);
            _pool.Release(b1);
            b1 = _pool.Get(6);
            _pool.Release(b1);

            // Get current stats
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(0, 1) },
                {  4, new Tuple<int, int>(0, 0) },
                {  6, new Tuple<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));

            // Get a new buffer; this should cause an alloc and a trim
            _pool.Get(3);
            _stats.Refresh();

            // Validate stats
            testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(0, 0) },
                {  4, new Tuple<int, int>(1, 0) },
                {  6, new Tuple<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(6, _stats.FreeBytes);
            Assert.AreEqual(4, _stats.UsedBytes);
            Assert.AreEqual(1, _stats.FreeCount);
            Assert.AreEqual(1, _stats.UsedCount);
        }

        /// <summary>
        /// Tests that we can reuse a free buffer in the pool
        /// </summary>
        [TestMethod]
        public void TestGet_Reuse()
        {
            // Get a buffer, and immediately release it
            byte[] b1 = _pool.Get(1);
            _pool.Release(b1);
            Assert.IsNotNull(b1);
            Assert.AreEqual(2, b1.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(2, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.UsedBytes);
            Assert.AreEqual(1, _stats.FreeCount);
            Assert.AreEqual(0, _stats.UsedCount);

            // Get another buffer of the same size as above. We should be able to reuse it
            byte[] b2 = _pool.Get(1);
            Assert.IsNotNull(b2);
            Assert.AreEqual(2, b2.Length);
            Assert.IsTrue(_pool.InUseValues.Contains(b2));
            _stats.Refresh();
            testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(1, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(2, _stats.UsedBytes);
            Assert.AreEqual(0, _stats.FreeCount);
            Assert.AreEqual(1, _stats.UsedCount);
        }

        /// <summary>
        /// Get via Alloc - exception on max size hard cap
        /// </summary>
        [TestMethod]
        public void TestGet_AllocFailure()
        {
            TestPool pool = new TestPool(4, 5);
            pool.Get(4);
            try
            {
              pool.Get(4);
              Assert.Fail();
            }
            catch (PoolSizeViolationException)
            {
                // This is expected
            }
        }

        /// <summary>
        /// Test a simple release
        /// </summary>
        [TestMethod]
        public void TestRelease()
        {
            // Get a buffer - causes an alloc
            byte[] b1 = _pool.Get(1);

            // Release this buffer
            _pool.Release(b1);

            // Verify stats
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(2, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.UsedBytes);
            Assert.AreEqual(1, _stats.FreeCount);
            Assert.AreEqual(0, _stats.UsedCount);
        }

        /// <summary>
        /// Test out Release(), when it should free the value, instead of adding to the pool
        /// </summary>
        [TestMethod]
        public void TestRelease_Free()
        {
            // Get a set of buffers that bump up above the max size
            _pool.Get(6);

            // Get and release another buffer. this should cause a free
            byte[] b3 = _pool.Get(6);
            _pool.Release(b3);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  6, new Tuple<int, int>(1, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(6, _stats.UsedBytes);
            Assert.AreEqual(0, _stats.FreeCount);
            Assert.AreEqual(1, _stats.UsedCount);
        }

        /// <summary>
        /// Test release on zero-sized pool
        /// </summary>
        [TestMethod]
        public void TestRelease_Free2()
        {
            // Create a new pool with a max size cap of zero.
            _pool = new TestPool(0, 10);
            _stats.SetPool(_pool);

            // get a buffer and release it - this should trigger the soft cap
            byte[] b1 = _pool.Get(4);
            _pool.Release(b1);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  4, new Tuple<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.UsedBytes);
            Assert.AreEqual(0, _stats.FreeCount);
            Assert.AreEqual(0, _stats.UsedCount);
        }

        /// <summary>
        /// Test release with bucket length constraints
        /// </summary>
        [TestMethod]
        public void TestRelease_BucketLengths()
        {
            _pool = new TestPool(int.MaxValue, int.MaxValue, MakeBucketSizeArray(2, 2));
            _stats.SetPool(_pool);

            byte[] b0 = _pool.Get(2);
            _pool.Get(2);
            _pool.Get(2);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(3, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(6, _stats.UsedBytes);
            Assert.AreEqual(3, _stats.UsedCount);
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.FreeCount);

            // Now release one of the buffers
            _pool.Release(b0);
            _stats.Refresh();
            testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(2, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(4, _stats.UsedBytes);
            Assert.AreEqual(2, _stats.UsedCount);
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.FreeCount);
        }

        /// <summary>
        /// Test releasing an 'unknown' value
        /// </summary>
        [TestMethod]
        public void TestRelease_UnknownValue()
        {
            // Get a buffer from the pool
            _pool.Get(1);

            // Allocate a buffer outside the pool
            byte[] b2 = new byte[2];

            // Try to release this buffer to the pool
            _pool.Release(b2);

            // Verify stats
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(1, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(2, _stats.UsedBytes);
            Assert.AreEqual(0, _stats.FreeCount);
            Assert.AreEqual(1, _stats.UsedCount);
        }

        /// <summary>
        /// Test out release with non reusable values
        /// </summary>
        [TestMethod]
        public void TestRelease_NonReusable()
        {
            TestPool pool = new TestPool(100, 100, MakeBucketSizeArray(2, 3));
            _pool.Reusable = false;
            _stats.SetPool(pool);

            // Get a buffer, and then release it
            byte[] b1 = _pool.Get(2);
            _pool.Release(b1);

            // Verify stats
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.UsedBytes);
            Assert.AreEqual(0, _stats.FreeCount);
            Assert.AreEqual(0, _stats.UsedCount);
        }

        /// <summary>
        /// Test buffers outside the 'normal' bucket sizes
        /// </summary>
        [TestMethod]
        public void TestGetRelease_NonBucketSizes()
        {
            _pool = new TestPool(10, 10, MakeBucketSizeArray(2, 1, 4, 1, 6, 1));
            _stats.SetPool(_pool);

            _pool.Get(2);
            byte[] b1 = _pool.Get(7);
            _stats.Refresh();
            Assert.AreEqual(10, _stats.UsedBytes);
            Assert.AreEqual(2, _stats.UsedCount);
            _pool.Release(b1);
            _stats.Refresh();
            Assert.AreEqual(2, _stats.UsedBytes);
            Assert.AreEqual(1, _stats.UsedCount);
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.FreeCount);

            byte[] b2 = new byte[3];
            _pool.Release(b2);
            _stats.Refresh();
            Assert.AreEqual(2, _stats.UsedBytes);
            Assert.AreEqual(1, _stats.UsedCount);
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(0, _stats.FreeCount);
        }

        /// <summary>
        /// Test illegal arguments to get
        /// </summary>
        [TestMethod]
        public void TestGetWithErrors()
        {
            int[] sizes = new int[] { -1, 0 };
            foreach (int s in sizes)
            {
                try
                {
                    _pool.Get(s);
                    Assert.Fail("Failed size: " + s);
                }
                catch (InvalidSizeException)
                {
                    // This is expected
                }
            }
        }

        /// <summary>
        /// Test out TrimToNothing functionality
        /// </summary>
        [TestMethod]
        public void TestTrimToNothing()
        {
            // Alloc a buffer and then release it
            byte[] b1 = _pool.Get(1);
            _pool.Release(b1);
            _pool.Get(3);
            _stats.Refresh();
            Assert.AreEqual(2, _stats.FreeBytes);
            Assert.AreEqual(4, _stats.UsedBytes);

            // Trim the pool and check
            _pool.TrimToNothing();
            _stats.Refresh();
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(4, _stats.UsedBytes);
        }

        /// <summary>
        /// Test out TrimToSize functionality
        /// </summary>
        [TestMethod]
        public void TestTrimToSize()
        {
            _pool = new TestPool(100, 100, MakeBucketSizeArray(2, 2, 4, 2, 6, 2));
            _stats.SetPool(_pool);

            // Allocate and release multiple buffers
            byte[] b1;
            _pool.Get(2);
            b1 = _pool.Get(2);
            _pool.Release(b1);
            b1 = _pool.Get(6);
            _pool.Release(b1);
            b1 = _pool.Get(4);
            _pool.Release(b1);

            _stats.Refresh();
            Assert.AreEqual(12, _stats.FreeBytes);
            Assert.AreEqual(2, _stats.UsedBytes);

            // Perform a dummy trim - nothing should happen
            _pool.TrimToSize(100);
            _stats.Refresh();
            Assert.AreEqual(12, _stats.FreeBytes);
            Assert.AreEqual(2, _stats.UsedBytes);

            // Now perform the real trim
            _pool.TrimToSize(8);
            _stats.Refresh();
            Assert.AreEqual(6, _stats.FreeBytes);
            Assert.AreEqual(2, _stats.UsedBytes);
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(1, 0) },
                {  4, new Tuple<int, int>(0, 0) },
                {  6, new Tuple<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));

            // Perform another trim
            _pool.TrimToSize(1);
            _stats.Refresh();
            Assert.AreEqual(0, _stats.FreeBytes);
            Assert.AreEqual(2, _stats.UsedBytes);
            testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  2, new Tuple<int, int>(1, 0) },
                {  4, new Tuple<int, int>(0, 0) },
                {  6, new Tuple<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Test out CanAllocate functionality
        /// </summary>
        [TestMethod]
        public void Test_CanAllocate()
        {
            TestPool pool = new TestPool(4, 8);
            pool.Get(4);
            Assert.IsFalse(pool.IsMaxSizeSoftCapExceeded());
            Assert.IsTrue(pool.CanAllocate(2));
            pool.Get(2);
            Assert.IsTrue(pool.IsMaxSizeSoftCapExceeded());
            Assert.IsTrue(pool.CanAllocate(2));
            Assert.IsFalse(pool.CanAllocate(4));
        }

        private static Dictionary<int, int> MakeBucketSizeArray(params int[] args)
        {
            Preconditions.CheckArgument(args.Length % 2 == 0);
            Dictionary<int, int> bucketSizes = new Dictionary<int, int>();
            for (int i = 0; i < args.Length; i += 2) 
            {
                bucketSizes.Add(args[i], args[i + 1]);
            }

            return bucketSizes;
        }

        /// <summary>
        /// A simple test pool that allocates byte arrays, and always allocates buffers of double
        /// the size requested
        /// </summary>
        class TestPool : BasePool<byte[]>
        {
            public bool Reusable { get; set; }

            public TestPool(int maxPoolSizeSoftCap, int maxPoolSizeHardCap) : this(maxPoolSizeSoftCap, maxPoolSizeHardCap, null)
            {
            }

            public TestPool(
                int maxPoolSizeSoftCap,
                int maxPoolSizeHardCap,
                Dictionary<int, int> bucketSizes) : base(
                    new MockMemoryTrimmableRegistry(),
                    new PoolParams(maxPoolSizeSoftCap, maxPoolSizeHardCap, bucketSizes),
                    new MockPoolStatsTracker())
            {
                Reusable = true;
                Initialize();
            }

            protected internal override byte[] Alloc(int bucketedSize)
            {
                return new byte[bucketedSize];
            }

            protected internal override void Free(byte[] value)
            {
            }

            protected internal override bool IsReusable(byte[] value)
            {
                return Reusable;
            }

            /// <summary>
            /// Allocate the smallest even number than is greater than or equal to the requested size
            /// <param name="requestSize">The logical request size</param>
            /// @return the slightly higher size
            /// </summary>
            protected internal override int GetBucketedSize(int requestSize)
            {
                if (requestSize <= 0)
                {
                    throw new InvalidSizeException(requestSize);
                }

                return (requestSize % 2 == 0) ? requestSize : requestSize + 1;
            }

            protected internal override int GetBucketedSizeForValue(byte[] value)
            {
                return value.Length;
            }

            protected internal override int GetSizeInBytes(int bucketedSize)
            {
                return bucketedSize;
            }
        }
    }
}
