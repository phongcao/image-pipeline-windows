using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Tests for PooledByteBufferInputStream 
    /// </summary>
    [TestClass]
    public sealed class PooledByteBufferInputStreamTests : IDisposable
    {
        private static readonly byte[] BYTES = new byte[] { 1, 123, 20, 3, 6, 23, 1 };
        private PooledByteBufferInputStream _stream;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            IPooledByteBuffer buffer = new TrivialPooledByteBuffer(BYTES);
            _stream = new PooledByteBufferInputStream(buffer);
        }

        /// <summary>
        /// Test cleanup.
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }

        /// <summary>
        /// Tests basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic()
        {
            Assert.AreEqual(0, _stream._currentOffset);
            Assert.AreEqual(BYTES.Length, _stream.Length);
        }

        /// <summary>
        /// Tests out Length and Seek method
        /// </summary>
        [TestMethod]
        public void TestLength()
        {
            Assert.AreEqual(BYTES.Length, _stream.Length);
            _stream.Seek(3, SeekOrigin.Current);
            Assert.AreEqual(BYTES.Length - 3, _stream.Length - _stream.Position);
            _stream.Seek(BYTES.Length, SeekOrigin.Current);
            Assert.AreEqual(0, _stream.Length - _stream.Position);
        }

        /// <summary>
        /// Tests out the Seek method
        /// </summary>
        [TestMethod]
        public void TestSeek()
        {
            Assert.AreEqual(2, _stream.Seek(2, SeekOrigin.Current));
            Assert.AreEqual(2, _stream._currentOffset);

            Assert.AreEqual(5, _stream.Seek(3, SeekOrigin.Current));
            Assert.AreEqual(5, _stream._currentOffset);

            Assert.AreEqual(BYTES.Length, _stream.Seek(BYTES.Length, SeekOrigin.Current));
        }

        /// <summary>
        /// Tests out the Seek method with negative offset
        /// </summary>
        [TestMethod]
        public void TestSeekNegative()
        {
            _stream.Seek(5, SeekOrigin.Begin);
            _stream.Seek(-4, SeekOrigin.Current);
            Assert.AreEqual(1, _stream._currentOffset);
        }

        /// <summary>
        /// Tests the Read method with invalid params
        /// </summary>
        [TestMethod]
        public void TestReadWithErrors()
        {
            try
            {
                _stream.Read(new byte[64], 10, 55);
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
                // This is expected
            }
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void TestRead_ToByteArray()
        {
            byte[] buf = new byte[64];

            Assert.AreEqual(0, _stream.Read(buf, 0, 0));
            Assert.AreEqual(0, _stream._currentOffset);

            Assert.AreEqual(3, _stream.Read(buf, 0, 3));
            Assert.AreEqual(3, _stream._currentOffset);
            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(BYTES[i], buf[i]);
            }

            for (int i = 3; i < buf.Length; ++i)
            {
                Assert.AreEqual(0, buf[i]);
            }

            int available = BYTES.Length - _stream._currentOffset;
            Assert.AreEqual(available, _stream.Read(buf, 3, available + 1));
            Assert.AreEqual(BYTES.Length, _stream._currentOffset);
            for (int i = 0; i < available; ++i)
            {
                Assert.AreEqual(BYTES[i], buf[i]);
            }

            Assert.AreEqual(0, _stream.Read(buf, 0, 1));
            Assert.AreEqual(BYTES.Length, _stream._currentOffset);
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void TestRead_ToByteArray2()
        {
            byte[] buf = new byte[BYTES.Length + 10];
            Assert.AreEqual(BYTES.Length, _stream.Read(buf, 0, buf.Length));
            for (int i = 0; i < BYTES.Length; ++i)
            {
                Assert.AreEqual(BYTES[i], buf[i]);
            }
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void testRead_ToByteArray3()
        {
            byte[] buf = new byte[BYTES.Length - 1];
            Assert.AreEqual(buf.Length, _stream.Read(buf, 0, buf.Length));
            Assert.AreEqual(buf.Length, _stream._currentOffset);
            for (int i = 0; i < buf.Length; ++i)
            {
                Assert.AreEqual(BYTES[i], buf[i]);
            }
        }

        /// <summary>
        /// Tests out the empty stream creation
        /// </summary>
        [TestMethod]
        public void TestCreateEmptyStream()
        {
            byte[] buf = new byte[100];
            PooledByteBufferInputStream inputStream = new PooledByteBufferInputStream(
              new TrivialPooledByteBuffer(new byte[] { }));
            Assert.AreEqual(0, inputStream.Read(buf, 0, 1));
        }

        /// <summary>
        /// Tests out the stream creation after being closed
        /// </summary>
        [TestMethod]
        public void TestCreatingStreamAfterClose()
        {
            IPooledByteBuffer buffer = new TrivialPooledByteBuffer(new byte[] { });
            buffer.Dispose();
            try
            {
                new PooledByteBufferInputStream(buffer);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // This is expected
            }
        }
    }
}
