using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Provides the custom implemetation for <see cref="IMemoryCacheTracker"/>.
    /// </summary>
    public class MemoryCacheTrackerImpl : IMemoryCacheTracker
    {
        private Action _onCacheHitFunc;
        private Action _onCacheMissFunc;
        private Action _onCachePutFunc;

        /// <summary>
        /// Instantiates the <see cref="MemoryCacheTrackerImpl"/>.
        /// </summary>
        public MemoryCacheTrackerImpl(
            Action onCacheHitFunc,
            Action onCacheMissFunc,
            Action onCachePutFunc)
        {
            _onCacheHitFunc = onCacheHitFunc;
            _onCacheMissFunc = onCacheMissFunc;
            _onCachePutFunc = onCachePutFunc;
        }

        /// <summary>
        /// On cache hit.
        /// </summary>
        public void OnCacheHit()
        {
            _onCacheHitFunc();
        }

        /// <summary>
        /// On cache miss.
        /// </summary>
        public void OnCacheMiss()
        {
            _onCacheMissFunc();
        }

        /// <summary>
        /// On cache put.
        /// </summary>
        public void OnCachePut()
        {
            _onCachePutFunc();
        }
    }
}
