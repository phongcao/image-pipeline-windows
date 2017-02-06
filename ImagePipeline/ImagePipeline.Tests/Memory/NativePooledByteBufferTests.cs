using FBCore.Common.References;
using ImagePipeline.Memory;
using ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Basic tests for <see cref="NativePooledByteBuffer"/>
    /// </summary>
    [TestClass]
    public sealed class NativePooledByteBufferTests : IDisposable
    {
        private static readonly byte[] BYTES = new byte[] { 1, 4, 5, 0, 100, 34, 0, 1, 2, 2 };
        private static readonly int BUFFER_LENGTH = BYTES.Length - 2;

        private NativeMemoryChunkPool _pool;
        private NativeMemoryChunk _chunk;
        private NativePooledByteBuffer _pooledByteBuffer;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _chunk = new FakeNativeMemoryChunk(BYTES.Length);
            _chunk.Write(0, BYTES, 0, BYTES.Length);
            _pool = new FakeNativeMemoryChunkPool();
            var poolRef = CloseableReference<NativeMemoryChunk>.of(_chunk, _pool);
            _pooledByteBuffer = new NativePooledByteBuffer(
                poolRef,
                BUFFER_LENGTH);

            poolRef.Dispose();
        }

        /// <summary>
        /// Test cleanup.
        /// </summary>
        public void Dispose()
        {
            _pooledByteBuffer.Dispose();
            _chunk.Dispose();
        }

        /// <summary>
        /// Tests basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic()
        {
            Assert.IsFalse(_pooledByteBuffer.IsClosed);
            Assert.AreSame(_chunk, _pooledByteBuffer._bufRef.Get());
            Assert.AreEqual(BUFFER_LENGTH, _pooledByteBuffer.Size);
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void TestSimpleRead()
        {
            for (int i = 0; i < 100; ++i)
            {
                int offset = i % BUFFER_LENGTH;
                Assert.AreEqual(BYTES[offset], _pooledByteBuffer.Read(offset));
            }
        }

        /// <summary>
        /// Tests out the Read method in case out of bound
        /// </summary>
        [TestMethod]
        public void TestSimpleReadOutOfBounds()
        {
            try
            {
                _pooledByteBuffer.Read(BUFFER_LENGTH);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // This is expected
            }
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void TestRangeRead()
        {
            byte[] readBuf = new byte[BUFFER_LENGTH];
            _pooledByteBuffer.Read(1, readBuf, 1, BUFFER_LENGTH - 2);
            Assert.AreEqual(0, readBuf[0]);
            Assert.AreEqual(0, readBuf[BUFFER_LENGTH - 1]);
            for (int i = 1; i < BUFFER_LENGTH - 1; ++i)
            {
                Assert.AreEqual(BYTES[i], readBuf[i]);
            }
        }

        /// <summary>
        /// Tests out the Read method in case out of bound
        /// </summary>
        [TestMethod]
        public void TestRangeReadOutOfBounds()
        {
            try
            {
                byte[] readBuf = new byte[BUFFER_LENGTH];
                _pooledByteBuffer.Read(1, readBuf, 0, BUFFER_LENGTH);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // This is expected
            }
        }

        /// <summary>
        /// Tests out the Read method using stream
        /// </summary>
        [TestMethod]
        public void TestReadFromStream()
        {
            Stream inputStream = new PooledByteBufferInputStream(_pooledByteBuffer);
            byte[] tmp = new byte[BUFFER_LENGTH + 1];
            int bytesRead = inputStream.Read(tmp, 0, tmp.Length);
            Assert.AreEqual(BUFFER_LENGTH, bytesRead);
            for (int i = 0; i<BUFFER_LENGTH; i++)
            {
                Assert.AreEqual(BYTES[i], tmp[i]);
            }

            Assert.AreEqual(-1, inputStream.ReadByte());
        }

        /// <summary>
        /// Tests out the Dispose method
        /// </summary>
        [TestMethod]
        public void TestClose()
        {
            MockPoolStatsTracker statsTracker = (MockPoolStatsTracker)_pool._poolStatsTracker;
            Assert.AreEqual(0, statsTracker.FreeCallCount);
            _pooledByteBuffer.Dispose();
            Assert.IsTrue(_pooledByteBuffer.IsClosed);
            Assert.IsNull(_pooledByteBuffer._bufRef);
            Assert.AreEqual(1, statsTracker.FreeCallCount);
        }

        /// <summary>
        /// Tests out the ClosedException
        /// </summary>
        [TestMethod]
        public void TestGettingSizeAfterClose()
        {
            try
            {
                _pooledByteBuffer.Dispose();
                var size = _pooledByteBuffer.Size;
                Assert.Fail();
            }
            catch (ClosedException)
            {
                // This is expected
            }
        }
    }
}
