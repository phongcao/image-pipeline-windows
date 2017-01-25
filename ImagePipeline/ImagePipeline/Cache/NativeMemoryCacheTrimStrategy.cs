using FBCore.Common.Memory;
using System.Diagnostics;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// CountingMemoryCache eviction strategy appropriate for caches that store resources off the Dalvik
    /// heap.
    ///
    /// <para />In case of OnCloseToDalvikHeapLimit nothing will be done. In case of other trim types
    /// eviction queue of the cache will be cleared.
    /// </summary>
    public class NativeMemoryCacheTrimStrategy : ICacheTrimStrategy
    {
        /// <summary>
        /// Instantiates the <see cref="NativeMemoryCacheTrimStrategy"/>
        /// </summary>
        public NativeMemoryCacheTrimStrategy()
        {
        }

        /// <summary>
        /// Specifies the trimming strategy for the cache.
        /// </summary>
        public double GetTrimRatio(double trimType)
        {
            if (trimType == MemoryTrimType.OnAppBackgrounded ||
                trimType == MemoryTrimType.OnSystemLowMemoryWhileAppInForeground ||
                trimType == MemoryTrimType.OnSystemLowMemoryWhileAppInBackground)
            {
                return 1;
            }

            if (trimType != MemoryTrimType.OnCloseToDalvikHeapLimit)
            {
                Debug.WriteLine($"unknown trim type: { trimType }");
            }

            return 0;
        }
    }
}
