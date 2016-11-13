using ImagePipeline.Cache;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ImagePipelineBase.Tests.ImagePipeline.Cache
{
    /// <summary>
    /// Tests for <see cref="CountingLruMap{K, V}"/>
    /// </summary>
    [TestClass]
    public class CountingLruMapTests
    {
        private CountingLruMap<string, object> _countingLruMap;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            ValueDescriptorHelper<object> valueDescriptor = 
                new ValueDescriptorHelper<object>(v => (int)v);

            _countingLruMap = new CountingLruMap<string, object>(valueDescriptor);
        }

        /// <summary>
        /// Tests the initial state
        /// </summary>
        [TestMethod]
        public void TestInitialState()
        {
            Assert.AreEqual(0, _countingLruMap.Count);
            Assert.AreEqual(0, _countingLruMap.SizeInBytes);
        }

        /// <summary>
        /// Tests out the Put method
        /// </summary>
        [TestMethod]
        public void TestPut()
        {
            // Last inserted element should be last in the queue
            _countingLruMap.Put("key1", 110);
            Assert.AreEqual(1, _countingLruMap.Count);
            Assert.AreEqual(110, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1");
            AssertValueOrder(110);

            _countingLruMap.Put("key2", 120);
            Assert.AreEqual(2, _countingLruMap.Count);
            Assert.AreEqual(230, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2");
            AssertValueOrder(110, 120);

            _countingLruMap.Put("key3", 130);
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);
        }

        /// <summary>
        /// Tests out the put method with same key
        /// </summary>
        [TestMethod]
        public void TestPut_SameKeyTwice()
        {
            _countingLruMap.Put("key1", 110);
            _countingLruMap.Put("key2", 120);
            _countingLruMap.Put("key3", 130);

            // Last inserted element should be last in the queue
            _countingLruMap.Put("key2", 150);
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(390, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key3", "key2");
            AssertValueOrder(110, 130, 150);
        }

        /// <summary>
        /// Tests out the Get method
        /// </summary>
        [TestMethod]
        public void TestGet()
        {
            _countingLruMap.Put("key1", 110);
            _countingLruMap.Put("key2", 120);
            _countingLruMap.Put("key3", 130);

            // Get shouldn't affect the ordering, nor the size
            Assert.AreEqual(120, _countingLruMap.Get("key2"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);

            Assert.AreEqual(110, _countingLruMap.Get("key1"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);

            Assert.AreEqual(null, _countingLruMap.Get("key4"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);

            Assert.AreEqual(130, _countingLruMap.Get("key3"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);
        }

        /// <summary>
        /// Tests out the Contains method
        /// </summary>
        [TestMethod]
        public void TestContains()
        {
            _countingLruMap.Put("key1", 110);
            _countingLruMap.Put("key2", 120);
            _countingLruMap.Put("key3", 130);

            // Contains shouldn't affect the ordering, nor the size
            Assert.IsTrue(_countingLruMap.Contains("key2"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);

            Assert.IsTrue(_countingLruMap.Contains("key1"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);

            Assert.IsFalse(_countingLruMap.Contains("key4"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);

            Assert.IsTrue(_countingLruMap.Contains("key3"));
            Assert.AreEqual(3, _countingLruMap.Count);
            Assert.AreEqual(360, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);
        }

        /// <summary>
        /// Tests out the Remove method
        /// </summary>
        [TestMethod]
        public void TestRemove()
        {
            _countingLruMap.Put("key1", 110);
            _countingLruMap.Put("key2", 120);
            _countingLruMap.Put("key3", 130);

            Assert.AreEqual(120, _countingLruMap.Remove("key2"));
            Assert.AreEqual(2, _countingLruMap.Count);
            Assert.AreEqual(240, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key3");
            AssertValueOrder(110, 130);

            Assert.AreEqual(130, _countingLruMap.Remove("key3"));
            Assert.AreEqual(1, _countingLruMap.Count);
            Assert.AreEqual(110, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1");
            AssertValueOrder(110);

            Assert.AreEqual(null, _countingLruMap.Remove("key4"));
            Assert.AreEqual(1, _countingLruMap.Count);
            Assert.AreEqual(110, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1");
            AssertValueOrder(110);

            Assert.AreEqual(110, _countingLruMap.Remove("key1"));
            Assert.AreEqual(0, _countingLruMap.Count);
            Assert.AreEqual(0, _countingLruMap.SizeInBytes);
            AssertKeyOrder();
            AssertValueOrder();
        }

        /// <summary>
        /// Tests out the RemoveAll method
        /// </summary>
        [TestMethod]
        public void TestRemoveAll()
        {
            _countingLruMap.Put("key1", 110);
            _countingLruMap.Put("key2", 120);
            _countingLruMap.Put("key3", 130);
            _countingLruMap.Put("key4", 140);

            _countingLruMap.RemoveAll(
                new Predicate<string>(k => k.Equals("key2") || k.Equals("key3")));

            Assert.AreEqual(2, _countingLruMap.Count);
            Assert.AreEqual(250, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key4");
            AssertValueOrder(110, 140);
        }

        /// <summary>
        /// Tests out the Clear method
        /// </summary>
        [TestMethod]
        public void TestClear()
        {
            _countingLruMap.Put("key1", 110);
            _countingLruMap.Put("key2", 120);
            _countingLruMap.Put("key3", 130);
            _countingLruMap.Put("key4", 140);

            _countingLruMap.Clear();
            Assert.AreEqual(0, _countingLruMap.Count);
            Assert.AreEqual(0, _countingLruMap.SizeInBytes);
            AssertKeyOrder();
            AssertValueOrder();
        }

        /// <summary>
        /// Tests out the GetMatchingEntries method
        /// </summary>
        [TestMethod]
        public void TestGetMatchingEntries()
        {
            _countingLruMap.Put("key1", 110);
            _countingLruMap.Put("key2", 120);
            _countingLruMap.Put("key3", 130);
            _countingLruMap.Put("key4", 140);

            IList<KeyValuePair<string, object>> entries = _countingLruMap.GetMatchingEntries(
                new Predicate<string>(k => k.Equals("key2") || k.Equals("key3")));

            Assert.IsNotNull(entries);
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("key2", entries[0].Key);
            Assert.AreEqual(120, entries[0].Value);
            Assert.AreEqual("key3", entries[1].Key);
            Assert.AreEqual(130, entries[1].Value);

            // Getting entries should not affect the order nor the size
            Assert.AreEqual(4, _countingLruMap.Count);
            Assert.AreEqual(500, _countingLruMap.SizeInBytes);
            AssertKeyOrder("key1", "key2", "key3", "key4");
            AssertValueOrder(110, 120, 130, 140);
        }

        /// <summary>
        /// Tests out the GetFirstKey method
        /// </summary>
        [TestMethod]
        public void TestGetFirstKey()
        {
            _countingLruMap.Put("key1", 110);
            AssertKeyOrder("key1");
            AssertValueOrder(110);
            Assert.AreEqual("key1", _countingLruMap.FirstKey);

            _countingLruMap.Put("key2", 120);
            AssertKeyOrder("key1", "key2");
            AssertValueOrder(110, 120);
            Assert.AreEqual("key1", _countingLruMap.FirstKey);

            _countingLruMap.Put("key3", 130);
            AssertKeyOrder("key1", "key2", "key3");
            AssertValueOrder(110, 120, 130);
            Assert.AreEqual("key1", _countingLruMap.FirstKey);

            _countingLruMap.Put("key1", 140);
            AssertKeyOrder("key2", "key3", "key1");
            AssertValueOrder(120, 130, 140);
            Assert.AreEqual("key2", _countingLruMap.FirstKey);

            _countingLruMap.Remove("key3");
            AssertKeyOrder("key2", "key1");
            AssertValueOrder(120, 140);
            Assert.AreEqual("key2", _countingLruMap.FirstKey);

            _countingLruMap.Remove("key2");
            AssertKeyOrder("key1");
            AssertValueOrder(140);
            Assert.AreEqual("key1", _countingLruMap.FirstKey);

            _countingLruMap.Remove("key1");
            AssertKeyOrder();
            AssertValueOrder();
            Assert.AreEqual(null, _countingLruMap.FirstKey);
        }

        private void AssertKeyOrder(params string[] expectedKeys)
        {
            CollectionAssert.AreEqual(expectedKeys, new Collection<string>(_countingLruMap.Keys));
        }

        private void AssertValueOrder(params int[] expectedValues)
        {
            CollectionAssert.AreEqual(expectedValues, new Collection<object>(_countingLruMap.Values));
        }
    }
}
