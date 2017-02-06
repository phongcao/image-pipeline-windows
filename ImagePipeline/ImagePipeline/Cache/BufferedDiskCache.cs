using BinaryResource;
using Cache.Common;
using Cache.Disk;
using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// BufferedDiskCache provides get and put operations to take care of scheduling 
    /// disk-cache read/writes.
    /// </summary>
    public class BufferedDiskCache
    {
        private readonly IFileCache _fileCache;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;
        private readonly PooledByteStreams _pooledByteStreams;
        private readonly IExecutorService _readExecutor;
        private readonly IExecutorService _writeExecutor;
        private readonly StagingArea _stagingArea;
        private readonly IImageCacheStatsTracker _imageCacheStatsTracker;

        /// <summary>
        /// Instantiates the <see cref="BufferedDiskCache"/>
        /// </summary>
        /// <param name="fileCache"></param>
        /// <param name="pooledByteBufferFactory"></param>
        /// <param name="pooledByteStreams"></param>
        /// <param name="readExecutor"></param>
        /// <param name="writeExecutor"></param>
        /// <param name="imageCacheStatsTracker"></param>
        public BufferedDiskCache(
            IFileCache fileCache,
            IPooledByteBufferFactory pooledByteBufferFactory,
            PooledByteStreams pooledByteStreams,
            IExecutorService readExecutor,
            IExecutorService writeExecutor,
            IImageCacheStatsTracker imageCacheStatsTracker)
        {
            _fileCache = fileCache;
            _pooledByteBufferFactory = pooledByteBufferFactory;
            _pooledByteStreams = pooledByteStreams;
            _readExecutor = readExecutor;
            _writeExecutor = writeExecutor;
            _imageCacheStatsTracker = imageCacheStatsTracker;
            _stagingArea = StagingArea.Instance;
        }

        /// <summary>
        /// Returns true if the key is in the in-memory key index.
        ///
        /// Not guaranteed to be correct. The cache may yet have this key even if 
        /// this returns false. But if it returns true, it definitely has it.
        ///
        /// Avoids a disk read.
        /// </summary>
        public bool ContainsSync(ICacheKey key)
        {
            return _stagingArea.ContainsKey(key) || _fileCache.HasKeySync(key);
        }

        /// <summary>
        /// Performs a key-value look up in the disk cache. If no value is found in 
        /// the staging area, then disk cache checks are scheduled on a background 
        /// thread. Any error manifests itself as a cache miss, i.e. the returned 
        /// Task resolves to false.
        /// <param name="key">The cache key.</param>
        /// @return Task that resolves to true if an element is found, or false 
        /// otherwise.
        /// </summary>
        public Task<bool> Contains(ICacheKey key)
        {
            if (ContainsSync(key))
            {
                return Task.FromResult(true);
            }

            return ContainsAsync(key);
        }

        private Task<bool> ContainsAsync(ICacheKey key)
        {
            try
            {
                return _readExecutor.Execute(() => CheckInStagingAreaAndFileCache(key));
            }
            catch (Exception)
            {
                // Log failure
                // TODO: 3697790
                Debug.WriteLine($"Failed to schedule disk-cache read for { key.ToString() }");
                throw;
            }
        }

        /// <summary>
        /// Performs disk cache check synchronously.
        /// <param name="key"></param>
        /// @return true if the key is found in disk cache else false
        /// </summary>
        public bool DiskCheckSync(ICacheKey key)
        {
            if (ContainsSync(key))
            {
                return true;
            }

            return CheckInStagingAreaAndFileCache(key);
        }

        /// <summary>
        /// Performs key-value look up in disk cache. If value is not found in disk 
        /// cache staging area then disk cache read is scheduled on background thread. 
        /// Any error manifests itself as cache miss, i.e. the returned task resolves 
        /// to null.
        /// <param name="key">The cache key.</param>
        /// <param name="isCancelled">The cancellation flag.</param>
        /// @return Task that resolves to cached element or null if one cannot be 
        /// retrieved; returned task never rethrows any exception.
        /// </summary>
        public Task<EncodedImage> Get(ICacheKey key, AtomicBoolean isCancelled)
        {
            EncodedImage pinnedImage = _stagingArea.Get(key);
            if (pinnedImage != null)
            {
                return FoundPinnedImage(key, pinnedImage);
            }

            return GetAsync(key, isCancelled);
        }

        /// <summary>
        /// Performs key-value loop up in staging area and file cache.
        /// Any error manifests itself as a miss, i.e. returns false.
        /// <param name="key">The cache key.</param>
        /// @return true if the image is found in staging area or File cache, 
        /// false if not found.
        /// </summary>
        private bool CheckInStagingAreaAndFileCache(ICacheKey key)
        {
            EncodedImage result = _stagingArea.Get(key);
            if (result != null)
            {
                result.Dispose();
                Debug.WriteLine($"Found image for { key.ToString() } in staging area");
                _imageCacheStatsTracker.OnStagingAreaHit();
                return true;
            }
            else
            {
                Debug.WriteLine($"Did not find image for { key.ToString() } in staging area");
                _imageCacheStatsTracker.OnStagingAreaMiss();

                try
                {
                    return _fileCache.HasKey(key);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private Task<EncodedImage> GetAsync(ICacheKey key, AtomicBoolean isCancelled)
        {
            try
            {
                if (isCancelled.Value)
                {
                    throw new OperationCanceledException();
                }

                EncodedImage result = _stagingArea.Get(key);
                if (result != null)
                {
                    Debug.WriteLine($"Found image for { key.ToString() } in staging area");
                    _imageCacheStatsTracker.OnStagingAreaHit();
                }
                else
                {
                    Debug.WriteLine($"Did not find image for { key.ToString() } in staging area");
                    _imageCacheStatsTracker.OnStagingAreaMiss();
                    try
                    {
                        IPooledByteBuffer buffer = ReadFromDiskCache(key);
                        CloseableReference<IPooledByteBuffer> reference = 
                            CloseableReference<IPooledByteBuffer>.of(buffer);

                        try
                        {
                            result = new EncodedImage(reference);
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                        }
                    }
                    catch (Exception)
                    {
                        return Task.FromResult(default(EncodedImage));
                    }
                }

                return Task.FromResult(result);
            }
            catch (Exception)
            {
                // Log failure
                // TODO: 3697790
                Debug.WriteLine($"Failed to schedule disk-cache read for { key.ToString() }");
                throw;
            }
        }

        /// <summary>
        /// Associates encodedImage with given key in disk cache. Disk write is performed on background
        /// thread, so the caller of this method is not blocked
        /// </summary>
        public Task Put(ICacheKey key, EncodedImage encodedImage)
        {
            Preconditions.CheckNotNull(key);
            Preconditions.CheckArgument(EncodedImage.IsValid(encodedImage));

            // Store encodedImage in staging area
            _stagingArea.Put(key, encodedImage);

            // Write to disk cache. This will be executed on background thread, so increment the ref count.
            // When this write completes (with success/failure), then we will bump down the ref count
            // again.
            EncodedImage finalEncodedImage = EncodedImage.CloneOrNull(encodedImage);
            try
            {
                return _writeExecutor.Execute(() =>
                {
                    try
                    {
                        WriteToDiskCache(key, finalEncodedImage);
                    }
                    finally
                    {
                        _stagingArea.Remove(key, finalEncodedImage);
                        EncodedImage.CloseSafely(finalEncodedImage);
                    }
                });
            }
            catch (Exception)
            {
                // We failed to enqueue cache write. Log failure and decrement ref count
                // TODO: 3697790
                Debug.WriteLine($"Failed to schedule disk-cache write for { key.ToString() }");
                _stagingArea.Remove(key, encodedImage);
                EncodedImage.CloseSafely(finalEncodedImage);
                throw;
            }
        }

        /// <summary>
        /// Removes the item from the disk cache and the staging area.
        /// </summary>
        public Task Remove(ICacheKey key)
        {
            Preconditions.CheckNotNull(key);
            _stagingArea.Remove(key);
            try
            {
                return _writeExecutor.Execute(() =>
                {
                    _stagingArea.Remove(key);
                    _fileCache.Remove(key);
                });
            }
            catch (Exception)
            {
                // Log failure
                // TODO: 3697790
                Debug.WriteLine($"Failed to schedule disk-cache remove for { key.ToString() }");
                throw;
            }
        }

        /// <summary>
        /// Clears the disk cache and the staging area.
        /// </summary>
        public Task ClearAll()
        {
            _stagingArea.ClearAll();
            try
            {
                return _writeExecutor.Execute(() =>
                {
                    _stagingArea.ClearAll();
                    _fileCache.ClearAll();
                });
            }
            catch (Exception)
            {
                // Log failure
                // TODO: 3697790
                Debug.WriteLine("Failed to schedule disk-cache clear");
                throw;
            }
        }

        private Task<EncodedImage> FoundPinnedImage(ICacheKey key, EncodedImage pinnedImage)
        {
            Debug.WriteLine($"Found image for { key.ToString() } in staging area");
            _imageCacheStatsTracker.OnStagingAreaHit();
            return Task.FromResult(pinnedImage);
        }

        /// <summary>
        /// Performs disk cache read. In case of any exception null is returned.
        /// </summary>
        private IPooledByteBuffer ReadFromDiskCache(ICacheKey key)
        {
            try
            {
                Debug.WriteLine($"Disk cache read for { key.ToString() }");
                IBinaryResource diskCacheResource = _fileCache.GetResource(key);
                if (diskCacheResource == null)
                {
                    Debug.WriteLine($"Disk cache miss for { key.ToString() }");
                    _imageCacheStatsTracker.OnDiskCacheMiss();
                    return null;
                }
                else
                {
                    Debug.WriteLine($"Found entry in disk cache for { key.ToString() }");
                    _imageCacheStatsTracker.OnDiskCacheHit();
                }

                IPooledByteBuffer byteBuffer;
                using (Stream inputStream = diskCacheResource.OpenStream())
                {
                    byteBuffer = _pooledByteBufferFactory.NewByteBuffer(
                        inputStream, 
                        (int)diskCacheResource.GetSize());
                }

                Debug.WriteLine($"Successful read from disk cache for { key.ToString() }");
                return byteBuffer;
            }
            catch (Exception)
            {
                // TODO: 3697790 log failures
                // TODO: 5258772 - uncomment line below
                // _fileCache.Remove(key);
                Debug.WriteLine($"Exception reading from cache for { key.ToString() }");
                _imageCacheStatsTracker.OnDiskCacheGetFail();
                throw;
            }
        }

        /// <summary>
        /// Writes to disk cache
        /// </summary>
        private void WriteToDiskCache(ICacheKey key, EncodedImage encodedImage)
        {
            Debug.WriteLine($"About to write to disk-cache for key { key.ToString() }");

            try
            {
                _fileCache.Insert(key, new WriterCallbackImpl(os =>
                {
                    _pooledByteStreams.Copy(encodedImage.GetInputStream(), os);
                }));

                Debug.WriteLine($"Successful disk-cache write for key { key.ToString() }");
            }
            catch (IOException)
            {
                // Log failure
                // TODO: 3697790
                Debug.WriteLine($"Failed to write to disk-cache for key { key.ToString() }");
            }
        }
    }
}
