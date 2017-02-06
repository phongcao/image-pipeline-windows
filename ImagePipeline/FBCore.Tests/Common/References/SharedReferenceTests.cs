using FBCore.Common.Internal;
using FBCore.Common.References;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Diagnostics;
using System.IO;

namespace FBCore.Tests.Common.References
{
    /// <summary>
    /// SharedReferenceTests
    /// </summary>
    [TestClass]
    public class SharedReferenceTests
    {
        /// <summary>
        /// Tests out the basic operations (isn't everything a basic operation?)
        /// </summary>
        [TestMethod]
        public void TestBasic()
        {
            // ref count = 1 after creation
            SharedReference<Thing> tRef = new SharedReference<Thing>(new Thing("abc"), THING_RELEASER);
            Assert.IsTrue(SharedReference<Thing>.IsValid(tRef));
            Assert.AreEqual(1, tRef.GetRefCountTestOnly());
            Thing t = tRef.Get();
            Assert.AreEqual("abc", t.Get());

            // Adding a reference increases the ref count
            tRef.AddReference();
            Assert.IsTrue(SharedReference<Thing>.IsValid(tRef));
            Assert.AreEqual(2, tRef.GetRefCountTestOnly());
            Assert.AreEqual(t, tRef.Get());
            Assert.AreEqual("abc", t.Get());

            // Deleting a reference drops the reference count
            tRef.DeleteReference();
            Assert.IsTrue(SharedReference<Thing>.IsValid(tRef));
            Assert.AreEqual(1, tRef.GetRefCountTestOnly());
            Assert.AreEqual(t, tRef.Get());
            Assert.AreEqual("abc", t.Get());

            // When the last reference is gone, the underlying object is disposed
            tRef.DeleteReference();
            Assert.IsFalse(SharedReference<Thing>.IsValid(tRef));
            Assert.AreEqual(0, tRef.GetRefCountTestOnly());

            // Adding a reference now should fail
            try
            {
                tRef.AddReference();
                Assert.Fail();
            }
            catch (NullReferenceException)
            {
                // This is expected
            }

            // So should deleting a reference
            try
            {
                tRef.DeleteReference();
                Assert.Fail();
            }
            catch (NullReferenceException)
            {
                // This is expected
            }

            // Null shared references are not 'valid'
            Assert.IsFalse(SharedReference<Thing>.IsValid(null));

            // test out exceptions during a close
            SharedReference<Thing> t2Ref = new SharedReference<Thing>(new Thing2("abc"), THING_RELEASER);

            // this should not throw
            t2Ref.DeleteReference();
        }

        /// <summary>
        /// Tests out the SharedReference constructor
        /// </summary>
        [TestMethod]
        public void TestNewSharedReference()
        {
            Thing thing = new Thing("abc");
            Assert.AreSame(thing, new SharedReference<Thing>(thing, THING_RELEASER).Get());
        }

        /// <summary>
        /// Tests out the custom released
        /// </summary>
        [TestMethod]
        public void TestCustomReleaser()
        {
            Thing thing = new Thing("abc");
            MockResourceReleaser<Thing> releaser = new MockResourceReleaser<Thing>();
            SharedReference<Thing> tRef = new SharedReference<Thing>(thing, releaser);
            tRef.DeleteReference();
            Assert.IsTrue(releaser.Released);
        }

        class Thing : IDisposable
        {
            private string _value;

            public Thing(string value)
            {
                _value = value;
            }

            public string Get()
            {
                return _value;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                _value = null;
            }
        }

        /// <summary>
        /// A subclass of Thing that throws an exception on close
        /// </summary>
        class Thing2 : Thing
        {
            public Thing2(string value) : base(value)
            {
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                throw new IOException("");
            }
        }

        class ThingResourceReleaser : IResourceReleaser<Thing>
        {
            void IResourceReleaser<Thing>.Release(Thing value)
            {
                try
                {
                    Closeables.Close(value, true);
                }
                catch (IOException ioe)
                {
                    // This should not happen
                    Debug.WriteLine($"{ ioe.Message } is NOT expected.");
                    Assert.Fail();
                }
            }
        }

        private readonly ThingResourceReleaser THING_RELEASER = new ThingResourceReleaser();
    }
}
