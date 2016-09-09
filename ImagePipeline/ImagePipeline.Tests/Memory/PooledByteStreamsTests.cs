using ImagePipelineBase.ImagePipeline.Memory;
using ImagePipelineTest.ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Diagnostics;
using System.IO;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Tests for PooledByteStreams 
    /// </summary>
    [TestClass]
    public class PooledByteStreamsTests
    {
        private const int POOLED_ARRAY_SIZE = 4;

        private MockByteArrayPool _byteArrayPool;
        private byte[] _pooledArray;

        private byte[] _data;
        private Stream _is;
        private MemoryStream _os;

        private PooledByteStreams _pooledByteStreams;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 115 };
            _pooledArray = new byte[4];
            _byteArrayPool = new MockByteArrayPool(_pooledArray);
            _is = new MemoryStream(_data);
            _os = new MemoryStream();
            _pooledByteStreams = new PooledByteStreams(_byteArrayPool, POOLED_ARRAY_SIZE);
        }

        /// <summary>
        /// Tests the pool usage
        /// </summary>
        [TestMethod]
        public void TestUsesPool()
        {
            _pooledByteStreams.Copy(_is, _os);
            Assert.AreEqual(1, _byteArrayPool.CallCount);
            Assert.AreEqual(1, _byteArrayPool.ReleaseCallCount);
        }

        /// <summary>
        /// Tests out the Release method on exception
        /// </summary>
        [TestMethod]
        public void TestReleasesOnException()
        {
            try
            {
                _pooledByteStreams.Copy(
                    _is,
                    new MockMemoryStream((b, o, c) =>
                    {
                        throw new IOException();
                    }));

                Assert.Fail();
            }
            catch (IOException ioe)
            {
                Debug.WriteLine($"{ioe.Message} is expected");
                Assert.AreEqual(1, _byteArrayPool.ReleaseCallCount);
            }
        }

        /// <summary>
        /// Tests out the copy method
        /// </summary>
        [TestMethod]
        public void TestCopiesData()
        {
            _pooledByteStreams.Copy(_is, _os);
            CollectionAssert.AreEqual(_data, _os.ToArray());
        }

        /// <summary>
        /// Tests out the Release method on exception with size
        /// </summary>
        [TestMethod]
        public void TestReleasesOnExceptionWithSize()
        {
            try
            {
                _pooledByteStreams.Copy(
                    _is,
                    new MockMemoryStream((b, o, c) =>
                    {
                        throw new IOException();
                    }), 
                    3);
                Assert.Fail();
            }
            catch (IOException ioe)
            {
                Debug.WriteLine($"{ioe.Message} is expected");
            }

            Assert.AreEqual(1, _byteArrayPool.ReleaseCallCount);
        }

        /// <summary>
        /// Tests out the copy method with size
        /// </summary>
        [TestMethod]
        public void TestCopiesDataWithSize()
        {
            _pooledByteStreams.Copy(_is, _os, 3);
            var subArray = new byte[3];
            Array.Copy(_data, 0, subArray, 0, subArray.Length);
            CollectionAssert.AreEqual(subArray, _os.ToArray());
        }
    }
}
