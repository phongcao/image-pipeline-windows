using FBCore.Common.Memory;
using System.Diagnostics;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// CountingMemoryCache eviction strategy appropriate for caches that store
    /// resources off the native heap.
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
