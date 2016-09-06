using FBCore.Common.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FBCore.Common.References
{
    /**
     * A smart pointer-like class for Java.
     *
     * <p>This class allows reference-counting semantics in a Java-friendlier way. A single object
     * can have any number of CloseableReferences pointing to it. When all of these have been closed,
     * the object either has its {@link Closeable#close} method called, if it implements
     * {@link Closeable}, or its designated {@link ResourceReleaser#release},
     * if it does not.
     *
     * <p>Callers can construct a CloseableReference wrapping a {@link Closeable} with:
     * <pre>
     * Closeable foo;
     * CloseableReference c = CloseableReference.of(foo);
     * </pre>
     * <p>Objects that do not implement Closeable can still use this class, but must supply a
     * {@link ResourceReleaser}:
     * <pre>
     * {@code
     * Object foo;
     * ResourceReleaser<Object> fooReleaser;
     * CloseableReference c = CloseableReference.of(foo, fooReleaser);
     * }
     * </pre>
     * <p>When making a logical copy, callers should call {@link #clone}:
     * <pre>
     * CloseableReference copy = c.clone();
     * </pre>
     * <p>
     * When each copy of CloseableReference is no longer needed, close should be called:
     * <pre>
     * copy.close();
     * c.close();
     * </pre>
     *
     * <p>As with any Closeable, try-finally semantics may be needed to ensure that close is called.
     * <p>Do not rely upon the finalizer; the purpose of this class is for expensive resources to
     * be released without waiting for the garbage collector. The finalizer will log an error if
     * the close method has not bee called.
     */
    public sealed class CloseableReference<T> : IDisposable
    {
        // Init lock
        private readonly object _referenceGate = new object();

        private static readonly DefaultResourceReleaser DEFAULT_CLOSEABLE_RELEASER = new DefaultResourceReleaser();

        private bool _isClosed = false;

        private readonly SharedReference<T> _sharedReference;

        /**
         * The caller should guarantee that reference count of sharedReference is not decreased to zero,
         * so that the reference is valid during execution of this method.
         */
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

        /**
         * Constructs a CloseableReference.
         *
         * <p>Returns null if the parameter is null.
         */
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

        /**
         * Constructs a CloseableReference (wrapping a SharedReference) of T with provided
         * ResourceReleaser<T>. If t is null, this will just return null.
         */
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

        /**
         * Closes this CloseableReference.
         *
         * <p>Decrements the reference count of the underlying object. If it is zero, the object
         * will be released.
         *
         * <p>This method is idempotent. Calling it multiple times on the same instance has no effect.
         */
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

        /**
         * Returns the underlying Closeable if this reference is not closed yet.
         * Otherwise IllegalStateException is thrown.
         */
        public T Get()
        {
            lock (_referenceGate)
            {
                Preconditions.CheckState(!_isClosed);
                return _sharedReference.Get();
            }
        }

        /**
         * Returns a new CloseableReference to the same underlying SharedReference. The SharedReference
         * ref-count is incremented.
         */
        public CloseableReference<T> Clone()
        {
            lock (_referenceGate)
            {
                Preconditions.CheckState(IsValid());
                return new CloseableReference<T>(_sharedReference);
            }
        }

        public CloseableReference<T> CloneOrNull()
        {
            lock (_referenceGate)
            {
                return IsValid() ? new CloseableReference<T>(_sharedReference) : null;
            }
        }

        /**
         * Checks if this closable-reference is valid i.e. is not closed.
         * @return true if the closeable reference is valid
         */
        public bool IsValid()
        {
            lock (_referenceGate)
            {
                return !_isClosed;
            }
        }

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

        /**
         * A test-only method to get the underlying references.
         *
         * <p><b>DO NOT USE in application code.</b>
         */
        public SharedReference<T> GetUnderlyingReferenceTestOnly()
        {
            lock (_referenceGate)
            {
                return _sharedReference;
            }
        }

        /**
         * Method used for tracking Closeables pointed by CloseableReference.
         * Use only for debugging and logging.
         */
        public int GetValueHash()
        {
            lock (_referenceGate)
            {
                return IsValid() ? _sharedReference.GetHashCode() : 0;
            }
        }

        /**
         * Checks if the closable-reference is valid i.e. is not null, and is not closed.
         * @return true if the closeable reference is valid
         */
        public static bool IsValid(CloseableReference<T> reference)
        {
            return reference != null && reference.IsValid();
        }

        /**
         * Returns the cloned reference if valid, null otherwise.
         *
         * @param ref the reference to clone
         */
        public static CloseableReference<T> CloneOrNull(CloseableReference<T> reference)
        {
            return (reference != null) ? reference.CloneOrNull() : null;
        }

        /**
         * Clones a collection of references and returns a list. Returns null if the list is null. If
         * the list is non-null, clones each reference. If a reference cannot be cloned due to already
         * being closed, the list will contain a null value in its place.
         *
         * @param refs the references to clone
         * @return the list of cloned references or null
         */
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

        /**
         * Closes the reference handling null.
         *
         * @param ref the reference to close
         */
        public static void CloseSafely(CloseableReference<T> reference)
        {
            if (reference != null)
            {
                reference.Dispose();
            }
        }

        /**
         * Closes the references in the iterable handling null.
         *
         * @param references the reference to close
         */
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
