using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Linq;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Basic tests for NativeMemoryChunk 
    /// </summary>
    [TestClass]
    public class NativeMemoryChunkTests
    {
        /// <summary>
        /// Tests the native memory allocation
        /// </summary>
        [TestMethod]
        public void TestAlloc()
        {
            int size = 128;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size))
            {
                Assert.IsNotNull(nativeMemoryChunk);
            }
        }

        /// <summary>
        /// Tests out the Dispose method
        /// </summary>
        [TestMethod]
        public void TestDispose()
        {
            int size = 128;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size))
            {
                nativeMemoryChunk.Dispose();
                Assert.IsTrue(nativeMemoryChunk.IsClosed);
            }
        }

        /// <summary>
        /// Tests out the Write method
        /// </summary>
        [TestMethod]
        public void TestWrite()
        {
            int size = 128;
            byte value = 1;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size))
            {
                byte[] src = Enumerable.Repeat(value, size).ToArray();
                Assert.AreEqual(size, nativeMemoryChunk.Write(0, src, 0, size));
            }
        }

        /// <summary>
        /// Tests out the Write method with the invalid count
        /// </summary>
        [TestMethod]
        public void TestWriteInvalidCount()
        {
            int size = 128;
            byte value = 1;
            int offset = 30;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size))
            {
                byte[] src = Enumerable.Repeat(value, size).ToArray();
                Assert.AreEqual(size - offset, nativeMemoryChunk.Write(offset, src, 0, size));
            }
        }

        /// <summary>
        /// Tests out the Write method with the invalid count
        /// </summary>
        [TestMethod]
        public void TestWriteInvalidCount2()
        {
            int size = 128;
            byte value = 1;
            int offset = 30;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size))
            {
                byte[] src = Enumerable.Repeat(value, size).ToArray();
                try
                {
                    nativeMemoryChunk.Write(0, src, offset, size);
                    Assert.Fail();
                }
                catch (ArgumentException)
                {
                    // This is expected
                }
            }
        }

        /// <summary>
        /// Tests out the Read byte method
        /// </summary>
        [TestMethod]
        public void TestReadByte()
        {
            int size = 128;
            byte value = 34;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size))
            {
                byte[] src = Enumerable.Repeat(value, size).ToArray();
                nativeMemoryChunk.Write(0, src, 0, size);
                Assert.AreEqual(value, nativeMemoryChunk.Read(0));
            }
        }

        /// <summary>
        /// Tests out the Read array method
        /// </summary>
        [TestMethod]
        public void TestReadArray()
        {
            int size = 128;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size))
            {
                byte[] src = new byte[size];
                for (int i = 0; i < size; ++i)
                {
                    src[i] = (byte)i;
                }
                     
                nativeMemoryChunk.Write(0, src, 0, size);
                byte[] dst = new byte[size];
                nativeMemoryChunk.Read(0, dst, 0, size);
                for (int i = 0; i < size; ++i)
                {
                    Assert.AreEqual(src[i], dst[i]);
                }
            }
        }

        /// <summary>
        /// Tests out the Copy method
        /// </summary>
        [TestMethod]
        public void TestCopy()
        {
            int size = 128;
            using (NativeMemoryChunk nativeMemoryChunk = new NativeMemoryChunk(size),
                                     nativeMemoryChunk2 = new NativeMemoryChunk(size))
            {
                byte[] src = new byte[size];
                for (int i = 0; i < size; ++i)
                {
                    src[i] = (byte)i;
                }

                nativeMemoryChunk.Write(0, src, 0, size);
                nativeMemoryChunk.Copy(0, nativeMemoryChunk2, 0, size);
                for (int i = 0; i < size; ++i)
                {
                    Assert.AreEqual(nativeMemoryChunk.Read(i), nativeMemoryChunk2.Read(i));
                }
            }
        }
    }
}
