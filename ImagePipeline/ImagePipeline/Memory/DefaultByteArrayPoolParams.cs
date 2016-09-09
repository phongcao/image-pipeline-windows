using FBCore.Common.Util;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Provides pool parameters (<see cref="PoolParams"/>) for common <see cref="IByteArrayPool"/>
    /// </summary>
    public static class DefaultByteArrayPoolParams
    {
        private const int DEFAULT_IO_BUFFER_SIZE = 16 * ByteConstants.KB;

        /// <summary>
        /// There are up to 5 simultaneous IO operations in new pipeline performed by:
        /// - 3 image-fetch threads
        /// - 2 image-cache threads
        /// We should be able to satisfy these requirements without any allocations
        /// </summary>
        private const int DEFAULT_BUCKET_SIZE = 5;
        private const int MAX_SIZE_SOFT_CAP = 5 * DEFAULT_IO_BUFFER_SIZE;

        /// <summary>
        /// We don't need hard cap here.
        /// </summary>
        private const int MAX_SIZE_HARD_CAP = 1 * ByteConstants.MB;

        /// <summary>
        /// Get default <see cref="PoolParams"/>.
        /// </summary>
        public static PoolParams Get()
        {
            // This pool supports only one bucket size: DEFAULT_IO_BUFFER_SIZE
            Dictionary<int, int> defaultBuckets = new Dictionary<int, int>();
            defaultBuckets.Add(DEFAULT_IO_BUFFER_SIZE, DEFAULT_BUCKET_SIZE);
            return new PoolParams(
                MAX_SIZE_SOFT_CAP,
                MAX_SIZE_HARD_CAP,
                defaultBuckets);
        }
    }
}
