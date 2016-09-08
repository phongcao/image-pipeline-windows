using FBCore.Common.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FBCore.Common.References
{
    /// <summary>
    /// A shared-reference class somewhat similar to c++ shared_ptr. The underlying value is reference
    /// counted, and when the count drops to zero, the underlying value is "disposed"
    /// <para />
    /// Unlike the c++ implementation, which provides for a bunch of syntactic sugar with copy
    /// constructors and destructors, Java does not provide the equivalents. So we instead have the
    /// explicit addReference() and deleteReference() calls, and we need to be extremely careful
    /// about using these in the presence of exceptions, or even otherwise.
    /// <para />
    /// Despite the extra (and clunky) method calls, this is still worthwhile in many cases to avoid
    /// the overhead of garbage collection.
    /// <para />
    /// The somewhat clunky rules are
    /// 1. If a function returns a SharedReference, it must guarantee that the reference count
    ///    is at least 1. In the case where a SharedReference is being constructed and returned,
    ///    the SharedReference constructor will already set the ref count to 1.
    /// 2. If a function calls another function with a shared-reference parameter,
    ///    2.1 The caller must ensure that the reference is valid for the duration of the
    ///        invocation.
    ///    2.2 The callee *is not* responsible for the cleanup of the reference.
    ///    2.3 If the callee wants to keep the reference around even after the call returns (for
    ///        example, stashing it away in a map), then it should "clone" the reference by invoking
    ///        <see cref="AddReference"/>
    /// <para />
    ///   Example #1 (function with a shared reference parameter):
    ///   void foo(SharedReference r, ...) {
    ///     // first assert that the reference is valid
    ///     Preconditions.checkArgument(SharedReference.isValid(r));
    ///     ...
    ///     // do something with the contents of r
    ///     ...
    ///     // do not increment/decrement the ref count
    ///   }
    /// <para />
    ///   Example #2 (function with a shared reference parameter that keeps around the shared ref)
    ///     void foo(SharedReference r, ...) {
    ///       // first assert that the reference is valid
    ///       Preconditions.checkArgument(SharedReference.isValid(r));
    ///       ...
    ///       // increment ref count
    ///       r.addReference();
    ///       // stash away the reference
    ///       ...
    ///       return;
    ///     }
    /// <para />
    ///   Example #3 (function with a shared reference parameter that passes along the reference to
    ///   another function)
    ///     void foo(SharedReference r, ...) {
    ///       // first assert that the reference is valid
    ///       Preconditions.checkArgument(SharedReference.isValid(r));
    ///       ...
    ///       bar(r, ...); // call to other function
    ///       ...
    ///     }
    /// <para />
    ///   Example #4 (function that returns a shared reference)
    ///     SharedReference foo(...) {
    ///       // do something
    ///       ...
    ///       // create a new shared reference (refcount automatically at 1)
    ///       SharedReference r = new SharedReference(x);
    ///       // return this shared reference
    ///       return r;
    ///     }
    /// <para />
    ///   Example #5 (function with a shared reference parameter that returns the shared reference)
    ///     void foo(SharedReference r, ...) {
    ///       // first assert that the reference is valid
    ///       Preconditions.checkArgument(SharedReference.isValid(r));
    ///       ...
    ///       // increment ref count before returning
    ///       r.addReference();
    ///       return r;
    ///     }
    /// </summary>
    public class SharedReference<T>
    {
        // Init lock
        private static readonly object _referenceGate = new object();

        // Keeps references to all live objects so finalization of those Objects always happens after
        // SharedReference first disposes of it. Note, this does not prevent CloseableReference's from
        // being finalized when the reference is no longer reachable.
        private static readonly Dictionary<object, int> _liveObjects = new Dictionary<object, int>();

        private T _value;

        private int _refCount;

        private readonly IResourceReleaser<T> _resourceReleaser;

        /// <summary>
        /// Construct a new shared-reference that will 'own' the supplied <code> value</code>.
        /// The reference count will be set to 1. When the reference count decreases to zero
        /// <code> resourceReleaser</code> will be used to release the <code> value</code>
        /// <param name="value">Non-null value to manage</param>
        /// <param name="resourceReleaser">Non-null ResourceReleaser for the value</param>
        /// </summary>
        public SharedReference(T value, IResourceReleaser<T> resourceReleaser)
        {
            _value = Preconditions.CheckNotNull(value);
            _resourceReleaser = Preconditions.CheckNotNull(resourceReleaser);
            _refCount = 1;
            AddLiveReference(value);
        }

        /// <summary>
        /// Increases the reference count of a live object in the static map. Adds it if it's not
        /// being held.
        ///
        /// <param name="value">The value to add.</param>
        /// </summary>
        private static void AddLiveReference(object value)
        {
            lock (_referenceGate)
            {
                int count = default(int);
                if (!_liveObjects.TryGetValue(value, out count))
                {
                    _liveObjects.Add(value, 1);
                }
                else
                {
                    _liveObjects[value] = count + 1;
                }
            }
        }

        /// <summary>
        /// Decreases the reference count of live object from the static map. Removes it if it's reference
        /// count has become 0.
        ///
        /// <param name="value">The value to remove.</param>
        /// </summary>
        private static void RemoveLiveReference(object value)
        {
            lock (_referenceGate)
            {
                int count = default(int);
                if (!_liveObjects.TryGetValue(value, out count))
                {
                    // Uh oh.
                    Debug.WriteLine($"SharedReference: No entry in sLiveObjects for value of type { value.GetType() }");
                }
                else if (count == 1)
                {
                    _liveObjects.Remove(value);
                }
                else
                {
                    _liveObjects[value] = count - 1;
                }
            }
        }

        /// <summary>
        /// Get the current referenced value. Null if there's no value.
        /// @return the referenced value
        /// </summary>
        public T Get()
        {
            lock (_referenceGate)
            {
                return _value;
            }
        }

        /// <summary>
        /// Checks if this shared-reference is valid i.e. its reference count is greater than zero.
        /// @return true if shared reference is valid
        /// </summary>
        public bool IsValid()
        {
            lock (_referenceGate)
            {
                return _refCount > 0;
            }
        }

        /// <summary>
        /// Checks if the shared-reference is valid i.e. its reference count is greater than zero
        /// @return true if the shared reference is valid
        /// </summary>
        public static bool IsValid(SharedReference<T> reference)
        {
            return reference != null && reference.IsValid();
        }

        /// <summary>
        /// Bump up the reference count for the shared reference
        /// Note: The reference must be valid (aka not null) at this point
        /// </summary>
        public void AddReference()
        {
            lock (_referenceGate)
            {
                EnsureValid();
                _refCount++;
            }
        }

        /// <summary>
        /// Decrement the reference count for the shared reference. If the reference count drops to zero,
        /// then dispose of the referenced value
        /// </summary>
        public void DeleteReference()
        {
            if (DecreaseRefCount() == 0)
            {
                T deleted;

                lock (_referenceGate)
                {
                    deleted = _value;
                    _value = default(T);
                }

                _resourceReleaser.Release(deleted);
                RemoveLiveReference(deleted);
            }
        }

        /// <summary>
        /// Decrements reference count for the shared reference. Returns value of mRefCount after
        /// decrementing
        /// </summary>
        private int DecreaseRefCount()
        {
            lock (_referenceGate)
            {
                EnsureValid();
                Preconditions.CheckArgument(_refCount > 0);

                _refCount--;
                return _refCount;
            }
        }

        /// <summary>
        /// Assert that there is a valid referenced value. Throw a NullReferenceException otherwise
        /// @throws NullReferenceException, if the reference is invalid (i.e.) the underlying value is null
        /// </summary>
        private void EnsureValid()
        {
            if (!IsValid(this))
            {
                throw new NullReferenceException();
            }
        }

        /// <summary>
        /// A test-only method to get the ref count
        /// DO NOT USE in regular code
        /// </summary>
        public int GetRefCountTestOnly()
        {
            lock (_referenceGate)
            {
                return _refCount;
            }
        }
    }
}
