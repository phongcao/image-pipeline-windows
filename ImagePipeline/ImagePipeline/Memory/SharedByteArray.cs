using FBCore.Common.Internal;
using FBCore.Common.Memory;
using FBCore.Common.References;
using ImagePipelineBase.ImagePipeline.Memory;
using System;
using System.Threading;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Maintains a shareable reference to a byte array.
    ///
    /// <para /> When accessing the shared array proper synchronization is guaranteed.
    /// Under hood the get method acquires an exclusive lock, which is released
    /// whenever the returned CloseableReference is closed.
    ///
    /// <para /> If the currently available byte array is too small for a request
    /// it is replaced with a bigger one.
    ///
    /// <para /> This class will also release the byte array if it is unused and
    /// collecting it can prevent an OOM.
    /// </summary>
    public class SharedByteArray : IMemoryTrimmable
    {
        private readonly object _arrayGate = new object();
        internal readonly int _minByteArraySize;
        internal readonly int _maxByteArraySize;

        /// <summary>
        /// The underlying byte array.
        ///
        /// <para /> If we receive a memory trim notification, or the runtime runs pre-OOM gc
        /// it will be cleared to reduce memory pressure.
        /// </summary>
        internal readonly OOMSoftReference<byte[]> _byteArraySoftRef;

        /// <summary>
        /// Synchronization primitive used by this implementation
        /// </summary>
        internal readonly SemaphoreSlim _semaphore;

        private readonly ResourceReleaser<byte[]> _resourceReleaser;

        /// <summary>
        /// Instantiates the <see cref="SharedByteArray"/>.
        /// </summary>
        /// <param name="memoryTrimmableRegistry">A class to be notified of system memory events.</param>
        /// <param name="args">The pool params</param>
        public SharedByteArray(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams args)
        {
            Preconditions.CheckNotNull(memoryTrimmableRegistry);
            Preconditions.CheckArgument(args.MinBucketSize > 0);
            Preconditions.CheckArgument(args.MaxBucketSize >= args.MinBucketSize);

            _maxByteArraySize = args.MaxBucketSize;
            _minByteArraySize = args.MinBucketSize;
            _byteArraySoftRef = new OOMSoftReference<byte[]>();
            _semaphore = new SemaphoreSlim(1, 1);
            _resourceReleaser = new ResourceReleaser<byte[]>(value =>
            {
                _semaphore.Release();
            });

            memoryTrimmableRegistry.RegisterMemoryTrimmable(this);
        }

        /// <summary>
        /// Get exclusive access to the byte array of size greater or equal to the passed one.
        ///
        /// <para /> Under the hood this method acquires an exclusive lock that is released when
        /// the returned reference is closed.
        /// </summary>
        public CloseableReference<byte[]> Get(int size)
        {
            Preconditions.CheckArgument(size > 0, "Size must be greater than zero");
            Preconditions.CheckArgument(size <= _maxByteArraySize, "Requested size is too big");
            _semaphore.Wait();

            try
            {
                byte[] byteArray = GetByteArray(size);
                return CloseableReference<byte[]>.of(byteArray, _resourceReleaser);
            }
            catch (Exception e)
            {
                _semaphore.Release();
                throw e;
            }
        }

        private byte[] GetByteArray(int requestedSize)
        {
            int bucketedSize = GetBucketedSize(requestedSize);
            byte[] byteArray = _byteArraySoftRef.Get();
            if (byteArray == null || byteArray.Length < bucketedSize)
            {
                byteArray = AllocateByteArray(bucketedSize);
            }

            return byteArray;
        }

        /// <summary>
        /// Responds to memory pressure by simply 'discarding' the local byte array if it is not used
        /// at the moment.
        ///
        /// <param name="trimType">Kind of trimming to perform (ignored)</param>
        /// </summary>
        public void Trim(double trimType)
        {
            if (!_semaphore.Wait(1))
            {
                return;
            }

            try
            {
                _byteArraySoftRef.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal int GetBucketedSize(int size)
        {
            size = Math.Max(size, _minByteArraySize);
            return HighestOneBit(size - 1) * 2;
        }

        private byte[] AllocateByteArray(int size)
        {
            lock (_arrayGate)
            {
                // Start with clearing reference and releasing currently owned byte array
                _byteArraySoftRef.Clear();
                byte[] byteArray = new byte[size];
                _byteArraySoftRef.Set(byteArray);
                return byteArray;
            }
        }

        /// <summary>
        /// http://www.mwsoft.jp/programming/java/java_lang_integer_highest_one_bit.html
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private int HighestOneBit(int i)
        {
            i |= (i >> 1);
            i |= (i >> 2);
            i |= (i >> 4);
            i |= (i >> 8);
            i |= (i >> 16);
            return i - (i >> 1);
        }
    }
}
