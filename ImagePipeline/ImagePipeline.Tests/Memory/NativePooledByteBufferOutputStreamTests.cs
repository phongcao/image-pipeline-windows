using FBCore.Common.References;
using ImagePipeline.Memory;
using ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Tests for NativePooledByteBufferOutputStream
    /// </summary>
    [TestClass]
    public class NativePooledByteBufferOutputStreamTests
    {
        private NativeMemoryChunkPool _pool;
        private byte[] _data;
        private PoolStats<NativeMemoryChunk> _stats;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _pool = new FakeNativeMemoryChunkPool();
            _stats = new PoolStats<NativeMemoryChunk>(_pool);
            _data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
        }

        /// <summary>
        /// Write out the contents of data into the output stream
        /// </summary>
        /// <param name="os"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private NativePooledByteBuffer DoWrite(NativePooledByteBufferOutputStream os, byte[] data)
        {
            for (int i = 0; i<data.Length; i++)
            {
                os.Write(data, i, 1);
            }

            return (NativePooledByteBuffer)os.ToByteBuffer();
        }

        /// <summary>
        /// Assert that the first 'length' bytes of expected are the same as those in 'actual'
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="length"></param>
        private void AssertArrayEquals(byte[] expected, byte[] actual, int length)
        {
            Assert.IsTrue(expected.Length >= length);
            Assert.IsTrue(actual.Length >= length);
            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        private byte[] GetBytes(NativePooledByteBuffer bb)
        {
            byte[] bytes = new byte[bb.Size];
            bb._bufRef.Get().Read(0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Tests the basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic_1()
        {
            NativePooledByteBufferOutputStream os1 = new NativePooledByteBufferOutputStream(_pool);
            NativePooledByteBuffer sb1 = DoWrite(os1, _data);
            Assert.AreEqual(16, sb1._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb1), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  32, new Tuple<int, int>(0, 0) },
                {  16, new Tuple<int, int>(1, 0) },
                {  8, new Tuple<int, int>(0, 1) },
                {  4, new Tuple<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests the basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic_2()
        {
            NativePooledByteBufferOutputStream os2 = new NativePooledByteBufferOutputStream(_pool, 8);
            NativePooledByteBuffer sb2 = DoWrite(os2, _data);
            Assert.AreEqual(16, sb2._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb2), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  32, new Tuple<int, int>(0, 0) },
                {  16, new Tuple<int, int>(1, 0) },
                {  8, new Tuple<int, int>(0, 1) },
                {  4, new Tuple<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests the basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic_3()
        {
            NativePooledByteBufferOutputStream os3 = new NativePooledByteBufferOutputStream(_pool, 16);
            NativePooledByteBuffer sb3 = DoWrite(os3, _data);
            Assert.AreEqual(16, sb3._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb3), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  32, new Tuple<int, int>(0, 0) },
                {  16, new Tuple<int, int>(1, 0) },
                {  8, new Tuple<int, int>(0, 0) },
                {  4, new Tuple<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests the basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic_4()
        {
            NativePooledByteBufferOutputStream os4 = new NativePooledByteBufferOutputStream(_pool, 32);
            NativePooledByteBuffer sb4 = DoWrite(os4, _data);
            Assert.AreEqual(32, sb4._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb4), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  32, new Tuple<int, int>(1, 0) },
                {  16, new Tuple<int, int>(0, 0) },
                {  8, new Tuple<int, int>(0, 0) },
                {  4, new Tuple<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests out the Dispose method
        /// </summary>
        [TestMethod]
        public void TestClose()
        {
            NativePooledByteBufferOutputStream os = new NativePooledByteBufferOutputStream(_pool);
            os.Dispose();
            _stats.Refresh();
            var testStat = new Dictionary<int, Tuple<int, int>>()
            {
                {  32, new Tuple<int, int>(0, 0) },
                {  16, new Tuple<int, int>(0, 0) },
                {  8, new Tuple<int, int>(0, 0) },
                {  4, new Tuple<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests out the ToByteBuffer method when there is exception
        /// </summary>
        [TestMethod]
        public void TestToByteBufException()
        {
            NativePooledByteBufferOutputStream os1 = new NativePooledByteBufferOutputStream(_pool);
            os1.Dispose();

            try
            {
                os1.ToByteBuffer();
                Assert.Fail();
            }
            catch (Exception)
            {
                // This is expected
            }
        }

        /// <summary>
        /// Tests out the Write method
        /// </summary>
        [TestMethod]
        public void TestWriteAfterToByteBuf()
        {
            NativePooledByteBufferOutputStream os1 = new NativePooledByteBufferOutputStream(_pool);
            byte[] _data1 = new byte[9];
            byte[] _data2 = new byte[3];
            Array.Copy(_data, _data1, _data1.Length);
            Array.Copy(_data, _data2, _data2.Length);
            NativePooledByteBuffer buf1 = DoWrite(os1, _data1);
            NativePooledByteBuffer buf2 = DoWrite(os1, _data2);
            Assert.AreEqual(12, buf2.Size);

            CloseableReference<NativeMemoryChunk> chunk = buf1._bufRef;
            Assert.AreEqual(3, chunk.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            os1.Dispose();
            buf1.Dispose();
            buf2.Dispose();
            Assert.AreEqual(0, chunk.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }
    }
}
