using FBCore.Common.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FBCore.Common.References
{
    /// <summary>
    /// A smart pointer-like class for Java.
    ///
    /// <para />This class allows reference-counting semantics in a Java-friendlier way. A single object
    /// can have any number of CloseableReferences pointing to it. When all of these have been closed,
    /// the object either has its <see cref="IDisposable.Dispose"/> method called, if it implements
    /// <see cref="IDisposable"/>, or its designated <see cref="IResourceReleaser&lt;T&gt;.Release"/>,
    /// if it does not.
    ///
    /// <para />Callers can construct a CloseableReference wrapping a <see cref="IDisposable"/> with:
    /// 
    /// Closeable foo;
    /// CloseableReference c = CloseableReference.of(foo);
    /// 
    /// <para />Objects that do not implement Closeable can still use this class, but must supply a
    /// <see cref="IResourceReleaser&lt;T&gt;"/>:
    /// 
    /// <code>
    ///   <![CDATA[ 
    ///         Object foo;
    ///         ResourceReleaser<object> fooReleaser;
    ///         CloseableReference c = CloseableReference.of(foo, fooReleaser);
    ///   ]]>  
    /// </code>
    /// 
    /// <para />When making a logical copy, callers should call <see cref="Clone"/>:
    /// 
    /// CloseableReference copy = c.Clone();
    /// 
    /// <para />
    /// When each copy of CloseableReference is no longer needed, close should be called:
    /// 
    /// copy.Dispose();
    /// c.Dispose();
    /// 
    ///
    /// <para />As with any Closeable, try-finally semantics may be needed to ensure that close is called.
    /// <para />Do not rely upon the finalizer; the purpose of this class is for expensive resources to
    /// be released without waiting for the garbage collector. The finalizer will log an error if
    /// the close method has not bee called.
    /// </summary>
    public sealed class CloseableReference<T> : IDisposable
    {
        // Init lock
        private readonly object _referenceGate = new object();

        private static readonly DefaultResourceReleaser DEFAULT_CLOSEABLE_RELEASER = new DefaultResourceReleaser();

        private bool _isClosed = false;

        private readonly SharedReference<T> _sharedReference;

        /// <summary>
        /// The caller should guarantee that reference count of sharedReference is not decreased to zero,
        /// so that the reference is valid during execution of this method.
        /// </summary>
        private CloseableReference(SharedReference<T> sharedReference)
        {
            _sharedReference = Preconditions.CheckNotNull(sharedReference);
            _sharedReference.AddReference();
        }

        private CloseableReference(T t, IResourceReleaser<T> resourceReleaser)
        {
            // Ref-count pre-set to 1
            _sharedReference = new SharedReference<T>(t, resourceReleaser);
        }

        /// <summary>
        /// Constructs a CloseableReference.
        ///
        /// <para />Returns null if the parameter is null.
        /// </summary>
        public static CloseableReference<T> of(T t)
        {
            if (t == null)
            {
                return null;
            }
            else
            {
                return new CloseableReference<T>(t, (IResourceReleaser<T>)DEFAULT_CLOSEABLE_RELEASER);
            }
        }

        /// <summary>
        /// Constructs a CloseableReference (wrapping a SharedReference) of T with provided
        /// <![CDATA[ ResourceReleaser<T>. If t is null, this will just return null. ]]>
        /// </summary>
        public static CloseableReference<T> of(T t, IResourceReleaser<T> resourceReleaser)
        {
            if (t == null)
            {
                return null;
            }
            else
            {
                return new CloseableReference<T>(t, resourceReleaser);
            }
        }

        /// <summary>
        /// Closes this CloseableReference.
        ///
        /// <para />Decrements the reference count of the underlying object. If it is zero, the object
        /// will be released.
        ///
        /// <para />This method is idempotent. Calling it multiple times on the same instance has no effect.
        /// </summary>
        public void Dispose()
        {
            lock (_referenceGate)
            {
                if (_isClosed)
                {
                    return;
                }

                _isClosed = true;
            }

            _sharedReference.DeleteReference();
        }

        /// <summary>
        /// Returns the underlying Closeable if this reference is not closed yet.
        /// Otherwise IllegalStateException is thrown.
        /// </summary>
        public T Get()
        {
            lock (_referenceGate)
            {
                Preconditions.CheckState(!_isClosed);
                return _sharedReference.Get();
            }
        }

        /// <summary>
        /// Returns a new CloseableReference to the same underlying SharedReference. The SharedReference
        /// ref-count is incremented.
        /// </summary>
        public CloseableReference<T> Clone()
        {
            lock (_referenceGate)
            {
                Preconditions.CheckState(IsValid());
                return new CloseableReference<T>(_sharedReference);
            }
        }

        /// <summary>
        /// Returns a new CloseableReference to the same underlying SharedReference or null if invalid. The 
        /// SharedReference ref-count is incremented.
        /// </summary>
        public CloseableReference<T> CloneOrNull()
        {
            lock (_referenceGate)
            {
                return IsValid() ? new CloseableReference<T>(_sharedReference) : null;
            }
        }

        /// <summary>
        /// Checks if this closable-reference is valid i.e. is not closed.
        /// @return true if the closeable reference is valid
        /// </summary>
        public bool IsValid()
        {
            lock (_referenceGate)
            {
                return !_isClosed;
            }
        }

        /// <summary>
        /// Perform cleanup operations on unmanaged resources held by the current object before 
        /// the object is destroyed
        /// </summary>
        ~CloseableReference()
        {
            try
            {
                // We put synchronized here so that lint doesn't warn about accessing mIsClosed, which is
                // guarded by this. Lint isn't aware of finalize semantics.
                lock (_referenceGate)
                {
                    if (_isClosed)
                    {
                        return;
                    }
                }

                Debug.WriteLine($"Finalized without closing: { GetHashCode() } { _sharedReference.GetHashCode() } (type = { _sharedReference.GetType() })");
                Dispose();
            }
            finally
            {
                // Do nothing
            }
        }

        /// <summary>
        /// A test-only method to get the underlying references.
        ///
        /// <para /><b>DO NOT USE in application code.</b>
        /// </summary>
        public SharedReference<T> GetUnderlyingReferenceTestOnly()
        {
            lock (_referenceGate)
            {
                return _sharedReference;
            }
        }

        /// <summary>
        /// Method used for tracking Closeables pointed by CloseableReference.
        /// Use only for debugging and logging.
        /// </summary>
        public int GetValueHash()
        {
            lock (_referenceGate)
            {
                return IsValid() ? _sharedReference.GetHashCode() : 0;
            }
        }

        /// <summary>
        /// Checks if the closable-reference is valid i.e. is not null, and is not closed.
        /// @return true if the closeable reference is valid
        /// </summary>
        public static bool IsValid(CloseableReference<T> reference)
        {
            return reference != null && reference.IsValid();
        }

        /// <summary>
        /// Returns the cloned reference if valid, null otherwise.
        ///
        /// <param name="reference">The reference to clone</param>
        /// </summary>
        public static CloseableReference<T> CloneOrNull(CloseableReference<T> reference)
        {
            return (reference != null) ? reference.CloneOrNull() : null;
        }

        /// <summary>
        /// Clones a collection of references and returns a list. Returns null if the list is null. If
        /// the list is non-null, clones each reference. If a reference cannot be cloned due to already
        /// being closed, the list will contain a null value in its place.
        ///
        /// <param name="refs">The references to clone</param>
        /// @return the list of cloned references or null
        /// </summary>
        public static List<CloseableReference<T>> CloneOrNull(IList<CloseableReference<T>> refs)
        {
            if (refs == null)
            {
                return null;
            }

            List<CloseableReference<T>> ret = new List<CloseableReference<T>>(refs.Count);
            foreach (CloseableReference<T> reference in refs)
            {
                ret.Add(CloneOrNull(reference));
            }

            return ret;
        }

        /// <summary>
        /// Closes the reference handling null.
        ///
        /// <param name="reference">The reference to close</param>
        /// </summary>
        public static void CloseSafely(CloseableReference<T> reference)
        {
            if (reference != null)
            {
                reference.Dispose();
            }
        }

        /// <summary>
        /// Closes the references in the iterable handling null.
        ///
        /// <param name="references">The reference to close</param>
        /// </summary>
        public static void CloseSafely(IEnumerable<CloseableReference<T>> references)
        {
            if (references != null)
            {
                foreach (CloseableReference<T> reference in references)
                {
                    CloseSafely(reference);
                }
            }
        }
    }
}
