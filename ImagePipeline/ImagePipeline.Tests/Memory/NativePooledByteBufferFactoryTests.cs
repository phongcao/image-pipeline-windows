using ImagePipeline.Memory;
using ImagePipeline.Testing;
using ImagePipelineTest.ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Basic tests for <see cref="NativePooledByteBufferFactory"/>
    /// </summary>
    [TestClass]
    public class NativePooledByteBufferFactoryTests
    {
        private NativeMemoryChunkPool _pool;
        private NativePooledByteBufferFactory _factory;
        private PoolStats<NativeMemoryChunk> _stats;
        PooledByteStreams _pooledByteStreams;
        private byte[] _data;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
            _pool = new FakeNativeMemoryChunkPool();
            _stats = new PoolStats<NativeMemoryChunk>(_pool);

            byte[] pooledByteArray = new byte[8];
            MockByteArrayPool byteArrayPool = new MockByteArrayPool(pooledByteArray);
            _pooledByteStreams = new PooledByteStreams(byteArrayPool, 8);
            _factory = new NativePooledByteBufferFactory(_pool, _pooledByteStreams);
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
        /// Tests out the NewByteBuffer method
        /// </summary>
        [TestMethod]
        public void TestNewByteBuf_1()
        {
            NativePooledByteBuffer sb1 = (NativePooledByteBuffer)_factory.NewByteBuffer(new MemoryStream(_data));
            Assert.AreEqual(16, sb1._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb1), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, KeyValuePair<int, int>>()
            {
                {  32, new KeyValuePair<int, int>(0, 0) },
                {  16, new KeyValuePair<int, int>(1, 0) },
                {  8, new KeyValuePair<int, int>(0, 1) },
                {  4, new KeyValuePair<int, int>(0, 1) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests out the NewByteBuffer method
        /// </summary>
        [TestMethod]
        public void TestNewByteBuf_2()
        {
            NativePooledByteBuffer sb2 = (NativePooledByteBuffer)_factory.NewByteBuffer(new MemoryStream(_data), 8);
            Assert.AreEqual(16, sb2._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb2), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, KeyValuePair<int, int>>()
            {
                {  32, new KeyValuePair<int, int>(0, 0) },
                {  16, new KeyValuePair<int, int>(1, 0) },
                {  8, new KeyValuePair<int, int>(0, 1) },
                {  4, new KeyValuePair<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests out the NewByteBuffer method
        /// </summary>
        [TestMethod]
        public void TestNewByteBuf_3()
        {
            NativePooledByteBuffer sb3 = (NativePooledByteBuffer)_factory.NewByteBuffer(
                new MemoryStream(_data), 16);
            Assert.AreEqual(16, sb3._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb3), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, KeyValuePair<int, int>>()
            {
                {  32, new KeyValuePair<int, int>(0, 0) },
                {  16, new KeyValuePair<int, int>(1, 0) },
                {  8, new KeyValuePair<int, int>(0, 0) },
                {  4, new KeyValuePair<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests out the NewByteBuffer method
        /// </summary>
        [TestMethod]
        public void TestNewByteBuf_4()
        {
            NativePooledByteBuffer sb4 = (NativePooledByteBuffer)_factory.NewByteBuffer(
                new MemoryStream(_data), 32);
            Assert.AreEqual(32, sb4._bufRef.Get().Size);
            AssertArrayEquals(_data, GetBytes(sb4), _data.Length);
            _stats.Refresh();
            var testStat = new Dictionary<int, KeyValuePair<int, int>>()
            {
                {  32, new KeyValuePair<int, int>(1, 0) },
                {  16, new KeyValuePair<int, int>(0, 0) },
                {  8, new KeyValuePair<int, int>(0, 0) },
                {  4, new KeyValuePair<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }

        /// <summary>
        /// Tests out the NewByteBuffer method
        /// </summary>
        [TestMethod]
        public void TestNewByteBuf_5()
        {
            NativePooledByteBuffer sb5 = (NativePooledByteBuffer)_factory.NewByteBuffer(5);
            Assert.AreEqual(8, sb5._bufRef.Get().Size);
            Assert.AreEqual(1, sb5._bufRef.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            _stats.Refresh();
            var testStat = new Dictionary<int, KeyValuePair<int, int>>()
            {
                {  32, new KeyValuePair<int, int>(0, 0) },
                {  16, new KeyValuePair<int, int>(0, 0) },
                {  8, new KeyValuePair<int, int>(1, 0) },
                {  4, new KeyValuePair<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
            sb5.Dispose();

            _stats.Refresh();
            testStat = new Dictionary<int, KeyValuePair<int, int>>()
            {
                {  32, new KeyValuePair<int, int>(0, 0) },
                {  16, new KeyValuePair<int, int>(0, 0) },
                {  8, new KeyValuePair<int, int>(0, 1) },
                {  4, new KeyValuePair<int, int>(0, 0) }
            };

            Assert.IsTrue(testStat.All(e => _stats.BucketStats.Contains(e)));
        }
    }
}
