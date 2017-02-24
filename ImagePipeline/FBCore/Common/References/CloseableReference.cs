using FBCore.Common.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FBCore.Common.References
{
    /// <summary>
    /// A smart pointer-like class for C#.
    ///
    /// <para />This class allows reference-counting semantics in a C#-friendlier way.
    /// A single object can have any number of CloseableReferences pointing to it.
    /// When all of these have been closed, the object either has its 
    /// <see cref="IDisposable.Dispose"/>method called, if it implements
    /// <see cref="IDisposable"/>, or its designated <see cref="IResourceReleaser{T}.Release"/>,
    /// if it does not.
    ///
    /// <para />Callers can construct a CloseableReference wrapping a
    /// <see cref="IDisposable"/> with:
    /// 
    /// IDisposable foo;
    /// CloseableReference{T} c = CloseableReference{T}.of(foo);
    /// 
    /// <para />Objects that do not implement IDisposable can still use this class,
    /// but must supply a <see cref="IResourceReleaser{T}"/>:
    /// 
    /// <code>
    ///   <![CDATA[ 
    ///         object foo;
    ///         IResourceReleaser<object> fooReleaser;
    ///         CloseableReference<IResourceReleaser<object>> c = 
    ///             CloseableReference<IResourceReleaser<object>>.of(foo, fooReleaser);
    ///   ]]>  
    /// </code>
    /// 
    /// <para />When making a logical copy, callers should call <see cref="Clone"/>:
    /// 
    /// CloseableReference{T} copy = c.Clone();
    /// 
    /// <para />
    /// When each copy of CloseableReference is no longer needed, close should be called:
    /// 
    /// copy.Dispose();
    /// c.Dispose();
    /// 
    ///
    /// <para />As with any IDisposable, try-finally semantics may be needed to ensure
    /// that close is called.
    /// <para />Do not rely upon the finalizer; the purpose of this class is for
    /// expensive resources to be released without waiting for the garbage collector.
    /// The finalizer will log an error if the Dispose method has not been called.
    /// </summary>
    public sealed class CloseableReference<T> : IDisposable
    {
        private readonly object _referenceGate = new object();

        private static readonly DefaultResourceReleaser<T> DEFAULT_CLOSEABLE_RELEASER = 
            new DefaultResourceReleaser<T>();

        private bool _isClosed = false;

        private readonly SharedReference<T> _sharedReference;

        /// <summary>
        /// The caller should guarantee that reference count of sharedReference is
        /// not decreased to zero, so that the reference is valid during execution
        /// of this method.
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
        /// Constructs a CloseableReference{T}. The argument must derive from
        /// IDisposable so that it can re-use the default IDisposable releaser.
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
                if (!(t is IDisposable))
                {
                    throw new ArgumentException("Argument is not IDisposable");
                }

                return new CloseableReference<T>(t, DEFAULT_CLOSEABLE_RELEASER);
            }
        }

        /// <summary>
        /// Constructs a CloseableReference (wrapping a SharedReference) of T with
        /// provided IResourceReleaser{T}. If t is null, this will just return null.
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
        /// <para />Decrements the reference count of the underlying object.
        /// If it is zero, the object will be released.
        ///
        /// <para />This method is idempotent. Calling it multiple times on
        /// the same instance has no effect.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Perform cleanup operations on unmanaged resources held by the current object
        /// before the object is destroyed.
        /// </summary>
        private void Dispose(bool disposing)
        {
            // We put synchronized here so that lint doesn't warn about accessing
            // _isClosed, which is guarded by this.
            // Lint isn't aware of finalize semantics.
            lock (_referenceGate)
            {
                if (_isClosed)
                {
                    return;
                }
            }

            if (!disposing)
            {
                Debug.WriteLine($"Finalized without closing: { GetHashCode() } { _sharedReference.GetHashCode() } (type = { _sharedReference.GetType() })");
            }

            _isClosed = true;
            _sharedReference.DeleteReference();
        }

        /// <summary>
        /// Returns the underlying Closeable if this reference is not closed yet.
        /// Otherwise InvalidOperationException is thrown.
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
        /// Returns a new CloseableReference to the same underlying SharedReference.
        /// The SharedReference ref-count is incremented.
        /// </summary>
        public CloseableReference<T> Clone()
        {
            lock (_referenceGate)
            {
                Preconditions.CheckState(Valid);
                return new CloseableReference<T>(_sharedReference);
            }
        }

        /// <summary>
        /// Returns a new CloseableReference to the same underlying SharedReference
        /// or null if invalid. The SharedReference ref-count is incremented.
        /// </summary>
        public CloseableReference<T> CloneOrNull()
        {
            lock (_referenceGate)
            {
                return Valid ? new CloseableReference<T>(_sharedReference) : null;
            }
        }

        /// <summary>
        /// Checks if this closable-reference is valid i.e. is not closed.
        /// </summary>
        /// <returns>true if the closeable reference is valid.</returns>
        public bool Valid
        {
            get
            {
                lock (_referenceGate)
                {
                    return !_isClosed;
                }
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
                return Valid ? _sharedReference.GetHashCode() : 0;
            }
        }

        /// <summary>
        /// Checks if the closable-reference is valid i.e. is not null,
        /// and is not closed.
        /// </summary>
        /// <returns>true if the closeable reference is valid.</returns>
        public static bool IsValid(CloseableReference<T> reference)
        {
            return reference != null && reference.Valid;
        }

        /// <summary>
        /// Returns the cloned reference if valid, null otherwise.
        /// </summary>
        /// <param name="reference">The reference to clone.</param>
        public static CloseableReference<T> CloneOrNull(CloseableReference<T> reference)
        {
            return (reference != null) ? reference.CloneOrNull() : null;
        }

        /// <summary>
        /// Clones a collection of references and returns a list.
        /// Returns null if the list is null.
        /// If the list is non-null, clones each reference.
        /// If a reference cannot be cloned due to already being closed,
        /// the list will contain a null value in its place.
        /// </summary>
        /// <param name="refs">The references to clone.</param>
        /// <returns>The list of cloned references or null.</returns>
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
        /// </summary>
        /// <param name="reference">The reference to close.</param>
        public static void CloseSafely(CloseableReference<T> reference)
        {
            if (reference != null)
            {
                reference.Dispose();
            }
        }

        /// <summary>
        /// Closes the references in the iterable handling null.
        /// </summary>
        /// <param name="references">The reference to close.</param>
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
