using ImagePipeline.Memory;
using ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Basic tests for BitmapCounter 
    /// </summary>
    [TestClass]
    public class BitmapCounterTests
    {
        private const int MAX_COUNT = 4;
        private const int MAX_SIZE = MAX_COUNT + 1;

        private BitmapCounter _bitmapCounter;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _bitmapCounter = new BitmapCounter(MAX_COUNT, MAX_SIZE);
        }

        /// <summary>
        /// Tests the basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic()
        {
            using (SoftwareBitmap bitmap1 = BitmapForSize(1),
                                  bitmap2 = BitmapForSize(2),
                                  bitmap3 = BitmapForSize(1))
            {
                AssertState(0, 0);
                Assert.IsTrue(_bitmapCounter.Increase(bitmap1));
                AssertState(1, 1);
                Assert.IsTrue(_bitmapCounter.Increase(bitmap2));
                AssertState(2, 3);
                _bitmapCounter.Decrease(bitmap3);
                AssertState(1, 2);
            }
        }

        /// <summary>
        /// Tests if the size decreases too much
        /// </summary>
        [TestMethod]
        public void TestDecreaseTooMuch()
        {
            using (SoftwareBitmap bitmap1 = BitmapForSize(1),
                                  bitmap2 = BitmapForSize(2))
            {
                try
                {
                    Assert.IsTrue(_bitmapCounter.Increase(bitmap1));
                    _bitmapCounter.Decrease(bitmap2);
                    Assert.Fail();
                }
                catch (ArgumentException)
                {
                    // This is expected
                }
            }
        }

        /// <summary>
        /// Tests if the bitmap count decreases too much
        /// </summary>
        [TestMethod]
        public void TestDecreaseTooMany()
        {
            using (SoftwareBitmap bitmap1 = BitmapForSize(2),
                                  bitmap2 = BitmapForSize(1),
                                  bitmap3 = BitmapForSize(1))
            {
                try
                {
                    Assert.IsTrue(_bitmapCounter.Increase(bitmap1));
                    _bitmapCounter.Decrease(bitmap2);
                    _bitmapCounter.Decrease(bitmap3);
                    Assert.Fail();
                }
                catch (ArgumentException)
                {
                    // This is expected
                }
            }
        }

        /// <summary>
        /// Tests the max size allocation
        /// </summary>
        [TestMethod]
        public void TestMaxSize()
        {
            using (SoftwareBitmap bitmap = BitmapForSize(MAX_SIZE))
            {
                Assert.IsTrue(_bitmapCounter.Increase(bitmap));
                AssertState(1, MAX_SIZE);
            }
        }

        /// <summary>
        /// Tests the max count allocation
        /// </summary>
        [TestMethod]
        public void TestMaxCount()
        {
            for (int i = 0; i < MAX_COUNT; ++i)
            {
                var bitmap = BitmapForSize(1);
                _bitmapCounter.Increase(bitmap);
                bitmap.Dispose();
            }

            AssertState(MAX_COUNT, MAX_COUNT);
        }

        /// <summary>
        /// Tests the allocation which exceeds the max size
        /// </summary>
        [TestMethod]
        public void IncreaseTooBigObject()
        {
            using (SoftwareBitmap bitmap = BitmapForSize(MAX_SIZE + 1))
            {
                Assert.IsFalse(_bitmapCounter.Increase(BitmapForSize(MAX_SIZE + 1)));
                AssertState(0, 0);
            }
        }

        /// <summary>
        /// Tests the allocation which exceeds the max count
        /// </summary>
        [TestMethod]
        public void IncreaseTooManyObjects()
        {
            for (int i = 0; i < MAX_COUNT; ++i)
            {
                var bitmap = BitmapForSize(1);
                _bitmapCounter.Increase(bitmap);
                bitmap.Dispose();
            }

            using (SoftwareBitmap bitmap = BitmapForSize(1))
            {
                Assert.IsFalse(_bitmapCounter.Increase(bitmap));
                AssertState(MAX_COUNT, MAX_COUNT);
            }
        }

        private void AssertState(int count, long size)
        {
            Assert.AreEqual(count, _bitmapCounter.GetCount());
            Assert.AreEqual(size, _bitmapCounter.GetSize());
        }

        private SoftwareBitmap BitmapForSize(int size)
        {
            return MockBitmapFactory.Create(size, 1, BitmapPixelFormat.Gray8);
        }
    }
}
