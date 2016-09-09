using ImagePipeline.Memory;
using ImagePipeline.Testing;
using ImagePipelineBase.ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.IO;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Tests for PooledByteArrayBufferedInputStreamTests 
    /// </summary>
    [TestClass]
    public class PooledByteArrayBufferedInputStreamTests
    {
        private MockResourceReleaser<byte[]> _resourceReleaser;
        private byte[] _buffer;
        private PooledByteArrayBufferedInputStream _pooledByteArrayBufferedInputStream;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _resourceReleaser = new MockResourceReleaser<byte[]>();
            byte[] bytes = new byte[256];
            for (int i = 0; i < 256; ++i)
            {
                bytes[i] = (byte)i;
            }

            Stream unbufferedStream = new MemoryStream(bytes);
            _buffer = new byte[10];
            _pooledByteArrayBufferedInputStream = new PooledByteArrayBufferedInputStream(
                unbufferedStream,
                _buffer,
                _resourceReleaser);
        }

        /// <summary>
        /// Tests out the Release method after being closed
        /// </summary>
        [TestMethod]
        public void TestReleaseOnClose()
        {
            _pooledByteArrayBufferedInputStream.Dispose();
            Assert.AreEqual(1, _resourceReleaser.ReleasedCallCount);
            _pooledByteArrayBufferedInputStream.Dispose();
            // we do not expect second close to release resource again,
            // the one checked bellow is the one that happened when close was called for the first time
            Assert.AreEqual(1, _resourceReleaser.ReleasedCallCount);
        }

        /// <summary>
        /// Tests out the Seek method
        /// </summary>
        [TestMethod]
        public void TestSeek()
        {
            // Buffer some data
            byte[] buf = new byte[1];
            _pooledByteArrayBufferedInputStream.Read(buf, 0, 1);
            Assert.AreEqual(100, _pooledByteArrayBufferedInputStream.Seek(99, SeekOrigin.Current));
            Assert.AreEqual(1, _pooledByteArrayBufferedInputStream.Read(buf, 0, 1));
            Assert.AreEqual(100, buf[0]);
        }

        /// <summary>
        /// Tests out the Seek method
        /// </summary>
        [TestMethod]
        public void TestSeek2()
        {
            byte[] buf = new byte[1];
            int i = 0;
            while (i < 256)
            {
                Assert.AreEqual(1, _pooledByteArrayBufferedInputStream.Read(buf, 0, 1));
                Assert.AreEqual(i, buf[0]);
                Assert.AreEqual(i + 8, _pooledByteArrayBufferedInputStream.Seek(7, SeekOrigin.Current));
                i += 8;
            }
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void TestReadWithByteArray()
        {
            byte[] readBuffer = new byte[5];
            Assert.AreEqual(5, _pooledByteArrayBufferedInputStream.Read(readBuffer, 0, readBuffer.Length));
            AssertFilledWithConsecutiveBytes(readBuffer, 0, 5, 0);
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void TestNonFullRead()
        {
            byte[] readBuffer = new byte[200];
            Assert.AreEqual(10, _pooledByteArrayBufferedInputStream.Read(readBuffer, 0, readBuffer.Length));
            AssertFilledWithConsecutiveBytes(readBuffer, 0, 10, 0);
            AssertFilledWithZeros(readBuffer, 10, 200);
        }

        /// <summary>
        /// Tests out the Read method
        /// </summary>
        [TestMethod]
        public void TestNonFullReadWithOffset()
        {
            byte[] readBuffer = new byte[200];
            Assert.AreEqual(10, _pooledByteArrayBufferedInputStream.Read(readBuffer, 45, 75));
            AssertFilledWithZeros(readBuffer, 0, 45);
            AssertFilledWithConsecutiveBytes(readBuffer, 45, 55, 0);
            AssertFilledWithZeros(readBuffer, 55, 200);
        }

        /// <summary>
        /// Given byte array, asserts that bytes in [startOffset, endOffset) range are all zeroed;
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        private static void AssertFilledWithZeros(
            byte[] byteArray,
            int startOffset,
            int endOffset)
        {
            for (int i = startOffset; i < endOffset; ++i)
            {
                Assert.AreEqual(0, byteArray[i]);
            }
        }

        /// <summary>
        /// Given byte array, asserts that each byte in (startOffset, endOffset) range has value equal 
        /// to value of previous byte plus one(mod 255) and byteArray[startOffset] is equal to firstByte.
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        /// <param name="firstByte"></param>
        private static void AssertFilledWithConsecutiveBytes(
            byte[] byteArray,
            int startOffset,
            int endOffset,
            int firstByte)
        {
            for (int i = startOffset; i < endOffset; ++i)
            {
                Assert.AreEqual((byte)firstByte++, byteArray[i]);
            }
        }
    }
}
