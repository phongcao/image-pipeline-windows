using FBCore.Common.Memory;
using FBCore.Common.References;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Manages a pool of reusable values of type V. The sizes of the values are
    /// described by the type T.
    /// The pool supports two main operations
    /// <see cref="Get(int)"/> - returns a value of size that's the same or larger
    /// than the requested size.
    /// <see cref="Release(T)"/> - releases the value to the pool.
    /// <para />
    /// In addition, the pool subscribes to the memory manager, and responds to
    /// low-memory events via calls to <see cref="IMemoryTrimmable.Trim(double)"/>.
    /// Some percent (perhaps all) of the values in the pool are then 'freed'.
    /// <para />
    /// Known implementations: BasePool (GenericByteArrayPool, SingleByteArrayPool,
    /// BitmapPool)
    /// </summary>
    public interface IPool<T> : IResourceReleaser<T>, IMemoryTrimmable
    {
        /// <summary>
        /// Gets a 'value' of size 'T' (or larger) from the pool, if available.
        /// Allocates a new value if necessary.
        /// </summary>
        /// <param name="size">The logical size to allocate.</param>
        /// <returns>A new value.</returns>
        T Get(int size);

        /// <summary>
        /// Releases the given value to the pool.
        /// The pool may decide to
        ///  - reuse the value (for future <see cref="Get(int)"/> operations OR
        ///  - 'free' the value.
        /// </summary>
        /// <param name="value">The value to release to the pool.</param>
        new void Release(T value);
    }
}
