using FBCore.Common.Memory;
using System.Diagnostics;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// CountingMemoryCache eviction strategy appropriate for bitmap caches.
    ///
    /// <para />If run on KitKat or below, then this TrimStrategy behaves exactly as
    /// NativeMemoryCacheTrimStrategy. If run on Lollipop, then BitmapMemoryCacheTrimStrategy will trim
    /// cache in one additional case: when OnCloseToDalvikHeapLimit trim type is received, cache's
    /// eviction queue will be trimmed according to OnCloseToDalvikHeapLimit's suggested trim ratio.
    /// </summary>
    public class BitmapMemoryCacheTrimStrategy : ICacheTrimStrategy
    {
        /// <summary>
        /// Gets the trim ratio
        /// </summary>
        public double GetTrimRatio(double trimType)
        {
            if (trimType == MemoryTrimType.OnCloseToDalvikHeapLimit)
            {
                return 0;
            }

            if (trimType == MemoryTrimType.OnAppBackgrounded ||
                trimType == MemoryTrimType.OnSystemLowMemoryWhileAppInForeground ||
                trimType == MemoryTrimType.OnSystemLowMemoryWhileAppInBackground)
            {
                return 1;
            }

            Debug.WriteLine($"unknown trim type: { trimType }");
            return 0;
        }
    }
}
